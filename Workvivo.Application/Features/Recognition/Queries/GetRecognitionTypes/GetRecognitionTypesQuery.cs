using MediatR;
using Microsoft.EntityFrameworkCore;
using Workvivo.Application.Common.Messaging;
using Workvivo.Application.Features.Recognition.Dtos;
using Workvivo.Domain.Abstractions.Classes;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Domain.Entities.Recognition;

namespace Workvivo.Application.Features.Recognition.Queries.GetRecognitionTypes;

/// <summary>
/// The recognition categories on offer.
///
/// Cacheable and cached: a handful of rows that HR changes a few times a year, read
/// every time somebody opens the recognition form.
/// </summary>
public sealed record GetRecognitionTypesQuery : IQuery<IReadOnlyList<RecognitionTypeDto>>, ICacheableQuery
{
    public string CacheKey => "recognition:types";

    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(30);
}

public sealed class GetRecognitionTypesQueryHandler
    : IRequestHandler<GetRecognitionTypesQuery, IReadOnlyList<RecognitionTypeDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetRecognitionTypesQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<RecognitionTypeDto>> Handle(
        GetRecognitionTypesQuery request,
        CancellationToken cancellationToken)
    {
        var rows = await _unitOfWork.Repository<RecognitionType, Guid>()
            .GetAllQ()
            .Where(type => type.Is_Active)
            .OrderBy(type => type.Sort_Order)
            .ThenBy(type => type.Code)
            .Select(type => new
            {
                type.Id,
                type.Code,
                type.Name_Ar,
                type.Name_En,
                type.Description_Ar,
                type.Description_En,
                type.Badge_Icon,
                type.Badge_Color,
                type.Default_Points,
            })
            .ToListAsync(cancellationToken);

        return
        [
            .. rows.Select(row => new RecognitionTypeDto
            {
                Id = row.Id,
                Code = row.Code,
                Name = LocalizedText.Pick(row.Name_Ar, row.Name_En) ?? string.Empty,
                Description = LocalizedText.Pick(row.Description_Ar, row.Description_En),
                BadgeIcon = row.Badge_Icon,
                BadgeColor = row.Badge_Color,
                DefaultPoints = row.Default_Points,
            }),
        ];
    }
}
