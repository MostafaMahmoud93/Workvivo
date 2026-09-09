using MediatR;
using Microsoft.EntityFrameworkCore;
using Workvivo.Application.Common.Messaging;
using Workvivo.Application.Features.Organization.Dtos;
using Workvivo.Domain.Abstractions.Interfaces;

namespace Workvivo.Application.Features.Organization.Queries.GetOrganizationLookups;

/// <summary>
/// Every list the employee-directory filters need, in one call.
///
/// One request rather than four. The directory cannot render its filter bar until it
/// has all of them, so four parallel requests would just be four chances for one to be
/// slow - and the payload is a few hundred short rows.
/// </summary>
public sealed record GetOrganizationLookupsQuery : IQuery<OrganizationLookupsDto>, ICacheableQuery
{
    public string CacheKey => "org:lookups";

    // Departments and offices change a few times a year. Anything shorter would be
    // caching for the sake of it; anything longer makes a reorganisation look broken.
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(30);
}

public sealed class OrganizationLookupsDto
{
    public required IReadOnlyList<LookupItemDto> Departments { get; init; }
    public required IReadOnlyList<LookupItemDto> Teams { get; init; }
    public required IReadOnlyList<LookupItemDto> Locations { get; init; }
    public required IReadOnlyList<LookupItemDto> JobTitles { get; init; }
}

public sealed class GetOrganizationLookupsQueryHandler
    : IRequestHandler<GetOrganizationLookupsQuery, OrganizationLookupsDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetOrganizationLookupsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<OrganizationLookupsDto> Handle(
        GetOrganizationLookupsQuery request,
        CancellationToken cancellationToken)
    {
        var departments = await _unitOfWork.Repository<Department, Guid>()
            .GetAllQ()
            .Where(department => department.Is_Active)
            .OrderBy(department => department.Path)
            .Select(department => new LookupItemDto
            {
                Id = department.Id,
                Name = department.Name_En ?? department.Name_Ar,
                Secondary = department.Code,
            })
            .ToListAsync(cancellationToken);

        var teams = await _unitOfWork.Repository<Team, Guid>()
            .GetAllQ()
            .Where(team => team.Is_Active)
            .OrderBy(team => team.Name_Ar)
            .Select(team => new LookupItemDto
            {
                Id = team.Id,
                Name = team.Name_En ?? team.Name_Ar,
                Secondary = team.Department == null ? null : team.Department.Name_En,
            })
            .ToListAsync(cancellationToken);

        var locations = await _unitOfWork.Repository<Location, Guid>()
            .GetAllQ()
            .Where(location => location.Is_Active)
            .OrderBy(location => location.Name_Ar)
            .Select(location => new LookupItemDto
            {
                Id = location.Id,
                Name = location.Name_En ?? location.Name_Ar,
                Secondary = location.City,
            })
            .ToListAsync(cancellationToken);

        var jobTitles = await _unitOfWork.Repository<JobTitle, Guid>()
            .GetAllQ()
            .Where(jobTitle => jobTitle.Is_Active)
            .OrderBy(jobTitle => jobTitle.Name_Ar)
            .Select(jobTitle => new LookupItemDto
            {
                Id = jobTitle.Id,
                Name = jobTitle.Name_En ?? jobTitle.Name_Ar,
                Secondary = jobTitle.Grade,
            })
            .ToListAsync(cancellationToken);

        return new OrganizationLookupsDto
        {
            Departments = departments,
            Teams = teams,
            Locations = locations,
            JobTitles = jobTitles,
        };
    }
}
