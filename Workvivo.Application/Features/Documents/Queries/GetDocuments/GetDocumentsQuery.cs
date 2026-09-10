using MediatR;
using Microsoft.EntityFrameworkCore;
using Workvivo.Application.Common.Messaging;
using Workvivo.Application.Common.Paging;
using Workvivo.Application.Common.Security;
using Workvivo.Application.Features.Documents.Dtos;
using Workvivo.Domain.Abstractions.Classes;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Domain.Entities.Documents;
using Workvivo.Infrastructure.Seeding;

namespace Workvivo.Application.Features.Documents.Queries.GetDocuments;

/// <summary>
/// The document centre.
///
/// Audience-filtered like everything else. A document is the one content type where
/// getting that wrong has consequences beyond embarrassment - salary bands, a
/// restructuring plan, a legal letter - so the filter is applied in the query and
/// again when the bytes are served.
/// </summary>
public sealed class GetDocumentsQuery : PageRequest, IQuery<PagedResult<DocumentSummaryDto>>
{
    public Guid? CategoryId { get; set; }

    public string? Search { get; set; }
}

public sealed class GetDocumentsQueryHandler
    : IRequestHandler<GetDocumentsQuery, PagedResult<DocumentSummaryDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly CurrentEmployee _currentEmployee;
    private readonly IAudienceResolver _audience;
    private readonly IPermissionService _permissions;

    public GetDocumentsQueryHandler(
        IUnitOfWork unitOfWork,
        CurrentEmployee currentEmployee,
        IAudienceResolver audience,
        IPermissionService permissions)
    {
        _unitOfWork = unitOfWork;
        _currentEmployee = currentEmployee;
        _audience = audience;
        _permissions = permissions;
    }

    public async Task<PagedResult<DocumentSummaryDto>> Handle(
        GetDocumentsQuery request,
        CancellationToken cancellationToken)
    {
        var employeeId = await _currentEmployee.RequireIdAsync(cancellationToken);
        var keys = await _audience.ResolveKeysAsync(employeeId, cancellationToken);

        var canManage = await _permissions.HasPermissionAsync(
            _currentEmployee.UserId, PermissionKeys.DocumentManage, cancellationToken);

        var query = _unitOfWork.Repository<Document, Guid>()
            .GetAllQ()
            .Where(document => document.Is_Published)
            .Where(document => _unitOfWork.Repository<DocumentAudience, Guid>()
                .GetAllQ()
                .Any(audience => audience.Document_Id == document.Id
                    && keys.Contains(audience.Audience_Key)));

        if (request.CategoryId is { } categoryId)
        {
            query = query.Where(document => document.Category_Id == categoryId);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();

            query = query.Where(document =>
                document.Title_Ar.Contains(term)
                || (document.Title_En != null && document.Title_En.Contains(term)));
        }

        var page = await query
            .OrderByDescending(document => document.Published_At)
            .ThenBy(document => document.Id)
            .Select(document => new Row
            {
                Id = document.Id,
                CategoryId = document.Category_Id,
                CategoryNameAr = document.Category!.Name_Ar,
                CategoryNameEn = document.Category.Name_En,
                TitleAr = document.Title_Ar,
                TitleEn = document.Title_En,
                DescriptionAr = document.Description_Ar,
                DescriptionEn = document.Description_En,
                FileName = document.CurrentVersion == null
                    ? null
                    : document.CurrentVersion.File!.Original_File_Name,
                ContentType = document.CurrentVersion == null
                    ? null
                    : document.CurrentVersion.File!.Content_Type,
                SizeBytes = document.CurrentVersion == null
                    ? null
                    : document.CurrentVersion.File!.Size_Bytes,
                ScanStatus = document.CurrentVersion == null
                    ? null
                    : document.CurrentVersion.File!.Scan_Status,
                VersionNumber = document.CurrentVersion == null
                    ? 0
                    : document.CurrentVersion.Version_Number,
                DownloadCount = document.Download_Count,
                PublishedAt = document.Published_At,
                ReviewDate = document.Review_Date,
                OwnerDisplayName = document.Owner == null ? string.Empty : document.Owner.Display_Name,
                OwnerEmployeeId = document.Owner_Employee_Id,
            })
            .ToPagedResultAsync(request, cancellationToken);

        return page.Map(row => new DocumentSummaryDto
        {
            Id = row.Id,
            CategoryId = row.CategoryId,
            CategoryName = LocalizedText.Pick(row.CategoryNameAr, row.CategoryNameEn) ?? string.Empty,
            Title = LocalizedText.Pick(row.TitleAr, row.TitleEn) ?? string.Empty,
            Description = LocalizedText.Pick(row.DescriptionAr, row.DescriptionEn),
            FileName = row.FileName,
            ContentType = row.ContentType,
            SizeBytes = row.SizeBytes,
            VersionNumber = row.VersionNumber,
            DownloadCount = row.DownloadCount,
            PublishedAt = row.PublishedAt,
            ReviewDate = row.ReviewDate,
            OwnerDisplayName = row.OwnerDisplayName,

            // Only a scanned-clean file is offered. Pending is a real state on a
            // fresh upload, and offering it would mean serving bytes nothing has
            // looked at yet.
            IsDownloadable = row.ScanStatus is FileScanStatus.Clean or FileScanStatus.Skipped,

            CanManage = canManage || row.OwnerEmployeeId == employeeId,
        });
    }

    private sealed class Row
    {
        public Guid Id { get; init; }
        public Guid CategoryId { get; init; }
        public string? CategoryNameAr { get; init; }
        public string? CategoryNameEn { get; init; }
        public string? TitleAr { get; init; }
        public string? TitleEn { get; init; }
        public string? DescriptionAr { get; init; }
        public string? DescriptionEn { get; init; }
        public string? FileName { get; init; }
        public string? ContentType { get; init; }
        public long? SizeBytes { get; init; }
        public FileScanStatus? ScanStatus { get; init; }
        public int VersionNumber { get; init; }
        public int DownloadCount { get; init; }
        public DateTime? PublishedAt { get; init; }
        public DateOnly? ReviewDate { get; init; }
        public string OwnerDisplayName { get; init; } = string.Empty;
        public Guid OwnerEmployeeId { get; init; }
    }
}
