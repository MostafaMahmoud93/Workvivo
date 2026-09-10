using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Workvivo.Application.Common.Messaging;
using Workvivo.Application.Common.Security;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Domain.Entities.Documents;
using Workvivo.Domain.Exceptions;
using Workvivo.Domain.Models.Storage;
using Workvivo.Infrastructure.Seeding;

namespace Workvivo.Application.Features.Documents.Commands.UploadDocument;

/// <summary>
/// Publishes a document, or adds a new version to an existing one.
///
/// The stream is passed rather than an <c>IFormFile</c> so the handler has no
/// dependency on ASP.NET and the same command can be issued by an import job.
/// </summary>
public sealed record UploadDocumentCommand(
    Guid? DocumentId,
    Guid CategoryId,
    string TitleAr,
    string? TitleEn,
    string? DescriptionAr,
    string? DescriptionEn,
    DateOnly? ReviewDate,
    IReadOnlyList<Guid> AudienceDepartmentIds,
    Stream Content,
    string FileName,
    string ContentType,
    long Length,
    string? ChangeNote) : ICommand<Guid>;

public sealed class UploadDocumentCommandValidator : AbstractValidator<UploadDocumentCommand>
{
    public UploadDocumentCommandValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.TitleAr).NotEmpty().MaximumLength(200);
        RuleFor(x => x.TitleEn).MaximumLength(200);
        RuleFor(x => x.DescriptionAr).MaximumLength(1000);
        RuleFor(x => x.DescriptionEn).MaximumLength(1000);
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Length).GreaterThan(0).WithMessage("The file is empty.");
        RuleFor(x => x.ChangeNote).MaximumLength(1000);
    }
}

public sealed class UploadDocumentCommandHandler : IRequestHandler<UploadDocumentCommand, Guid>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly CurrentEmployee _currentEmployee;
    private readonly IPermissionService _permissions;
    private readonly IFileStorageService _storage;
    private readonly IFileScanner _scanner;
    private readonly IContentSanitizer _sanitizer;
    private readonly IDateTimeProvider _clock;

    public UploadDocumentCommandHandler(
        IUnitOfWork unitOfWork,
        CurrentEmployee currentEmployee,
        IPermissionService permissions,
        IFileStorageService storage,
        IFileScanner scanner,
        IContentSanitizer sanitizer,
        IDateTimeProvider clock)
    {
        _unitOfWork = unitOfWork;
        _currentEmployee = currentEmployee;
        _permissions = permissions;
        _storage = storage;
        _scanner = scanner;
        _sanitizer = sanitizer;
        _clock = clock;
    }

    public async Task<Guid> Handle(UploadDocumentCommand request, CancellationToken cancellationToken)
    {
        var employeeId = await _currentEmployee.RequireIdAsync(cancellationToken);

        if (!await _permissions.HasPermissionAsync(
                _currentEmployee.UserId, PermissionKeys.DocumentManage, cancellationToken))
        {
            throw new ForbiddenException(
                "You cannot publish documents.", PermissionKeys.DocumentManage);
        }

        var category = await _unitOfWork.Repository<DocumentCategory, Guid>()
            .GetAllQ()
            .AnyAsync(candidate => candidate.Id == request.CategoryId && candidate.Is_Active,
                cancellationToken);

        if (!category)
        {
            throw new NotFoundException(nameof(DocumentCategory), request.CategoryId);
        }

        var now = _clock.UtcNow;

        // Scanned before it is stored, not after.
        //
        // The order matters: a file written first and scanned second exists in the
        // store, however briefly, and any bug between the two leaves it there behind
        // nothing but an authorisation check. Refusing it before it lands means
        // there is no window at all.
        var scan = await _scanner.ScanAsync(request.Content, request.FileName, cancellationToken);

        if (!scan.IsClean)
        {
            throw new BusinessRuleException(
                "That file did not pass the malware scan.", "document.infected");
        }

        // The scanner consumed the stream. Rewinding is what lets the same bytes be
        // stored without buffering them a second time.
        if (request.Content.CanSeek)
        {
            request.Content.Position = 0;
        }

        var stored = await _storage.SaveAsync(
            new FileUploadRequest
            {
                Content = request.Content,
                OriginalFileName = request.FileName,
                DeclaredContentType = request.ContentType,
                Length = request.Length,
                Container = "documents",
                Category = FileCategory.Document,
            },
            cancellationToken);

        var asset = new FileAsset
        {
            Id = Guid.NewGuid(),
            Storage_Provider = stored.Provider,
            Storage_Key = stored.StorageKey,
            Original_File_Name = stored.OriginalFileName,

            // From the bytes, not from what the client declared. A .pdf that is
            // actually an HTML document would otherwise be served back with a content
            // type the browser will happily render as a page.
            Content_Type = stored.ContentType,
            Extension = stored.Extension,
            Size_Bytes = stored.SizeBytes,
            Checksum_Sha256 = stored.ChecksumSha256,
            Scan_Status = FileScanStatus.Clean,
            Scanned_At = now,
            Uploaded_By = _currentEmployee.UserId,
            Uploaded_At = now,
            Container = "documents",
            Is_Deleted = false,
        };

        await _unitOfWork.Repository<FileAsset, Guid>().AddAsync(asset);

        var document = request.DocumentId is { } documentId
            ? await _unitOfWork.Repository<Document, Guid>().FindByIDAsync(documentId)
                ?? throw new NotFoundException(nameof(Document), documentId)
            : new Document
            {
                Id = Guid.NewGuid(),
                Owner_Employee_Id = employeeId,
                Version_Count = 0,
                Download_Count = 0,
                Is_Deleted = false,
            };

        document.Category_Id = request.CategoryId;
        document.Title_Ar = _sanitizer.ToPlainText(request.TitleAr);
        document.Title_En = request.TitleEn is null ? null : _sanitizer.ToPlainText(request.TitleEn);
        document.Description_Ar = request.DescriptionAr is null
            ? null
            : _sanitizer.ToPlainText(request.DescriptionAr);
        document.Description_En = request.DescriptionEn is null
            ? null
            : _sanitizer.ToPlainText(request.DescriptionEn);
        document.Review_Date = request.ReviewDate;
        document.Is_Published = true;
        document.Published_At ??= now;

        if (request.DocumentId is null)
        {
            await _unitOfWork.Repository<Document, Guid>().AddAsync(document);
        }

        var version = new DocumentVersion
        {
            Id = Guid.NewGuid(),
            Document_Id = document.Id,
            File_Id = asset.Id,

            // Versions are numbered, not dated. "Version 3" is what a policy is cited
            // as in a meeting; a timestamp is not.
            Version_Number = document.Version_Count + 1,
            Change_Note = request.ChangeNote,

            // Who uploaded it and when are the audit columns the DbContext stamps for
            // every entity; a second pair here would be the same facts kept twice and
            // free to disagree.
            Effective_From = now,
            Is_Deleted = false,
        };

        await _unitOfWork.Repository<DocumentVersion, Guid>().AddAsync(version);

        document.Current_Version_Id = version.Id;
        document.Version_Count = version.Version_Number;

        await ReplaceAudiencesAsync(document, request.AudienceDepartmentIds, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return document.Id;
    }

    private async Task ReplaceAudiencesAsync(
        Document document,
        IReadOnlyList<Guid> departmentIds,
        CancellationToken cancellationToken)
    {
        var audiences = _unitOfWork.Repository<DocumentAudience, Guid>();

        var existing = await audiences.GetAllQ()
            .Where(audience => audience.Document_Id == document.Id)
            .ToListAsync(cancellationToken);

        foreach (var audience in existing)
        {
            audiences.DeleteByEntity(audience);
        }

        var targets = departmentIds.Distinct().ToArray();

        if (targets.Length == 0)
        {
            document.Audiences.Add(
                new DocumentAudience(document.Id, AudienceType.AllEmployees, null));
            return;
        }

        foreach (var departmentId in targets)
        {
            document.Audiences.Add(
                new DocumentAudience(document.Id, AudienceType.Department, departmentId));
        }
    }
}
