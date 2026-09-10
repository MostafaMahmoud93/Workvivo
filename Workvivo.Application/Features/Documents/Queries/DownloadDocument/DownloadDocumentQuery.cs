using MediatR;
using Microsoft.EntityFrameworkCore;
using Workvivo.Application.Common.Messaging;
using Workvivo.Application.Common.Security;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Domain.Entities.Documents;
using Workvivo.Domain.Exceptions;

namespace Workvivo.Application.Features.Documents.Queries.DownloadDocument;

/// <summary>An open stream and what the browser needs to save it.</summary>
public sealed class DocumentDownload
{
    public required Stream Content { get; init; }
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public long SizeBytes { get; init; }
}

/// <summary>
/// Fetches a document's bytes for somebody entitled to them.
///
/// Deliberately a query that streams rather than an endpoint that hands out a URL to
/// the store. The audience rules are re-evaluated here, at the moment the bytes are
/// read - a link issued when somebody was in Finance must stop working when they
/// leave it.
/// </summary>
public sealed record DownloadDocumentQuery(Guid DocumentId) : IQuery<DocumentDownload>;

public sealed class DownloadDocumentQueryHandler
    : IRequestHandler<DownloadDocumentQuery, DocumentDownload>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly CurrentEmployee _currentEmployee;
    private readonly IAudienceResolver _audience;
    private readonly IFileStorageService _storage;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public DownloadDocumentQueryHandler(
        IUnitOfWork unitOfWork,
        CurrentEmployee currentEmployee,
        IAudienceResolver audience,
        IFileStorageService storage,
        ICurrentUser currentUser,
        IDateTimeProvider clock)
    {
        _unitOfWork = unitOfWork;
        _currentEmployee = currentEmployee;
        _audience = audience;
        _storage = storage;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<DocumentDownload> Handle(
        DownloadDocumentQuery request,
        CancellationToken cancellationToken)
    {
        var employeeId = await _currentEmployee.RequireIdAsync(cancellationToken);
        var keys = await _audience.ResolveKeysAsync(employeeId, cancellationToken);

        var document = await _unitOfWork.Repository<Document, Guid>()
            .GetAllQ()
            .Where(candidate => candidate.Id == request.DocumentId && candidate.Is_Published)
            .Select(candidate => new
            {
                candidate.Id,
                VersionId = candidate.Current_Version_Id,
                File = candidate.CurrentVersion == null ? null : candidate.CurrentVersion.File,
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(Document), request.DocumentId);

        var entitled = await _unitOfWork.Repository<DocumentAudience, Guid>()
            .GetAllQ()
            .AnyAsync(
                audience => audience.Document_Id == document.Id
                    && keys.Contains(audience.Audience_Key),
                cancellationToken);

        // Not entitled reads as not found. Confirming that a document called
        // "Redundancy plan Q3" exists is most of the disclosure.
        if (!entitled)
        {
            throw new NotFoundException(nameof(Document), request.DocumentId);
        }

        if (document.File is not { } file)
        {
            throw new NotFoundException(nameof(DocumentVersion), request.DocumentId);
        }

        if (!file.IsServable)
        {
            throw new BusinessRuleException(
                "This file has not finished its security check.", "document.not-servable");
        }

        var content = await _storage.OpenReadAsync(file.Storage_Key, cancellationToken);

        await RecordDownloadAsync(document.Id, document.VersionId!.Value, employeeId, cancellationToken);

        return new DocumentDownload
        {
            Content = content,
            FileName = file.Original_File_Name,

            // The type derived from the bytes at upload, never the one the client
            // declared then or asks for now.
            ContentType = file.Content_Type,
            SizeBytes = file.Size_Bytes,
        };
    }

    /// <summary>
    /// Records who downloaded what.
    ///
    /// A document centre without this cannot answer "who has read the new policy",
    /// which is the question the people publishing policies actually have.
    /// </summary>
    private async Task RecordDownloadAsync(
        Guid documentId,
        Guid versionId,
        Guid employeeId,
        CancellationToken cancellationToken)
    {
        // The version is recorded, not just the document. "Who has read the new
        // policy" is a different question from "who has read the policy", and only
        // the version answers the first one.
        await _unitOfWork.Repository<DocumentDownloadLog, long>().AddAsync(new DocumentDownloadLog
        {
            Document_Id = documentId,
            Version_Id = versionId,
            Employee_Id = employeeId,
            Downloaded_At = _clock.UtcNow,
            Ip_Address = _currentUser.IpAddress,
            User_Agent = _currentUser.UserAgent,
            Is_Deleted = false,
        });

        await _unitOfWork.Repository<Document, Guid>().ExecuteUpdateAsync(
            candidate => candidate.Id == documentId,
            setters => setters.SetProperty(
                candidate => candidate.Download_Count,
                candidate => candidate.Download_Count + 1),
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
