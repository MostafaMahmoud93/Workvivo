using MediatR;
using Microsoft.EntityFrameworkCore;
using Workvivo.Application.Common.Messaging;
using Workvivo.Application.Features.Documents.Dtos;
using Workvivo.Domain.Abstractions.Classes;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Domain.Entities.Documents;

namespace Workvivo.Application.Features.Documents.Queries.GetDocumentCategories;

/// <summary>The document centre's categories, with how many published documents each holds.</summary>
public sealed record GetDocumentCategoriesQuery : IQuery<IReadOnlyList<DocumentCategoryDto>>;

public sealed class GetDocumentCategoriesQueryHandler
    : IRequestHandler<GetDocumentCategoriesQuery, IReadOnlyList<DocumentCategoryDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetDocumentCategoriesQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<DocumentCategoryDto>> Handle(
        GetDocumentCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        var rows = await _unitOfWork.Repository<DocumentCategory, Guid>()
            .GetAllQ()
            .Where(category => category.Is_Active)
            .OrderBy(category => category.Sort_Order)
            .Select(category => new
            {
                category.Id,
                category.Name_Ar,
                category.Name_En,
                category.Icon,

                // Counted here rather than denormalised: categories number in the
                // tens and the count is only shown on one screen, so a column to
                // maintain would be more drift than it is worth.
                DocumentCount = category.Documents.Count(document =>
                    document.Is_Published && !document.Is_Deleted),
            })
            .ToListAsync(cancellationToken);

        return
        [
            .. rows.Select(row => new DocumentCategoryDto
            {
                Id = row.Id,
                Name = LocalizedText.Pick(row.Name_Ar, row.Name_En) ?? string.Empty,
                Icon = row.Icon,
                DocumentCount = row.DocumentCount,
            }),
        ];
    }
}
