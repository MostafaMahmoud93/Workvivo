using MediatR;
using Microsoft.AspNetCore.Mvc;
using Workvivo.Application.Bases;
using Workvivo.Application.Common.Paging;
using Workvivo.Application.Features.Documents.Commands.UploadDocument;
using Workvivo.Application.Features.Documents.Dtos;
using Workvivo.Application.Features.Documents.Queries.DownloadDocument;
using Workvivo.Application.Features.Documents.Queries.GetDocumentCategories;
using Workvivo.Application.Features.Documents.Queries.GetDocuments;
using Workvivo.Infrastructure.Seeding;

namespace Workvivo.API.Controllers.Platform;

/// <summary>
/// The document centre.
///
/// The only place in the API that moves file bytes, and the rules that matter are
/// about who may read them rather than who may call the endpoint. Document.View is
/// the floor; the audience rules inside the handlers decide the rest, and they are
/// re-evaluated when the bytes are served rather than only when the list is built.
/// </summary>
public class DocumentsController : ApiControllersBase
{
    /// <summary>
    /// Largest document accepted, independent of the transport-level cap.
    ///
    /// The per-category limits in Storage:Validation are the real policy; this stops
    /// a body being buffered to disk before those get a chance to run.
    /// </summary>
    private const long MaxUploadBytes = 100L * 1024 * 1024;

    private readonly ISender _sender;

    public DocumentsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [Route(RouteClass.Documents.List)]
    [HasPermission(Permissions.Document.View)]
    public async Task<IActionResult> List(
        [FromQuery] GetDocumentsQuery query,
        CancellationToken cancellationToken) =>
        Ok(ApiResponse<PagedResult<DocumentSummaryDto>>.Ok(await _sender.Send(query, cancellationToken)));

    [HttpGet]
    [Route(RouteClass.Documents.Categories)]
    [HasPermission(Permissions.Document.View)]
    public async Task<IActionResult> Categories(CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyList<DocumentCategoryDto>>.Ok(
            await _sender.Send(new GetDocumentCategoriesQuery(), cancellationToken)));

    /// <summary>
    /// Publishes a document or adds a version.
    ///
    /// Multipart rather than base64 in JSON: a hundred-megabyte file becomes a
    /// hundred and thirty-three megabytes of string that has to be held in memory to
    /// be parsed.
    /// </summary>
    [HttpPost]
    [Route(RouteClass.Documents.Upload)]
    [HasPermission(Permissions.Document.Manage)]
    [RequestSizeLimit(MaxUploadBytes)]
    public async Task<IActionResult> Upload(
        [FromForm] UploadDocumentRequest request,
        CancellationToken cancellationToken)
    {
        if (request.File is not { Length: > 0 } file)
        {
            return BadRequest(ApiResponse.Fail("Choose a file to upload."));
        }

        // Opened rather than copied into memory. The handler scans and then stores
        // from the same stream, and ASP.NET has already buffered anything large to
        // disk for us.
        await using var content = file.OpenReadStream();

        var documentId = await _sender.Send(
            new UploadDocumentCommand(
                request.DocumentId,
                request.CategoryId,
                request.TitleAr,
                request.TitleEn,
                request.DescriptionAr,
                request.DescriptionEn,
                request.ReviewDate,
                request.AudienceDepartmentIds ?? [],
                content,
                file.FileName,
                file.ContentType,
                file.Length,
                request.ChangeNote),
            cancellationToken);

        return Ok(ApiResponse<Guid>.Ok(documentId));
    }

    /// <summary>
    /// Streams a document's bytes.
    ///
    /// Not wrapped in the ApiResponse envelope, because the response is the file. The
    /// content type is the one derived from the bytes at upload, and the disposition
    /// is always an attachment: serving an uploaded file inline lets an HTML document
    /// that slipped through run as a page on this origin.
    /// </summary>
    [HttpGet]
    [Route(RouteClass.Documents.Download)]
    [HasPermission(Permissions.Document.View)]
    public async Task<IActionResult> Download(Guid documentId, CancellationToken cancellationToken)
    {
        var download = await _sender.Send(new DownloadDocumentQuery(documentId), cancellationToken);

        // X-Content-Type-Options is already set globally by the security headers
        // middleware, which stops a browser sniffing past the declared type.
        return File(download.Content, download.ContentType, download.FileName);
    }
}

/// <summary>The multipart form behind an upload.</summary>
public sealed class UploadDocumentRequest
{
    public Guid? DocumentId { get; set; }

    public Guid CategoryId { get; set; }

    public string TitleAr { get; set; } = string.Empty;

    public string? TitleEn { get; set; }

    public string? DescriptionAr { get; set; }

    public string? DescriptionEn { get; set; }

    public DateOnly? ReviewDate { get; set; }

    public string? ChangeNote { get; set; }

    /// <summary>Empty means everybody.</summary>
    public IReadOnlyList<Guid>? AudienceDepartmentIds { get; set; }

    public IFormFile? File { get; set; }
}
