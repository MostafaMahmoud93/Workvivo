using MediatR;
using Microsoft.EntityFrameworkCore;
using Workvivo.Application.Common.Messaging;
using Workvivo.Application.Features.Employees.Dtos;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Domain.Entities.Organization;

namespace Workvivo.Application.Features.Employees.Queries.SearchEmployees;

/// <summary>
/// Type-ahead for the mention picker.
///
/// Separate from the directory query because the shape of the problem is different: it
/// runs on every keystroke, needs four fields rather than fifteen, and is hard-capped
/// so a one-letter term cannot ask for the whole company.
/// </summary>
public sealed record SearchEmployeesQuery(string Term, int Limit = 10)
    : IQuery<IReadOnlyList<EmployeeSuggestionDto>>;

public sealed class SearchEmployeesQueryHandler
    : IRequestHandler<SearchEmployeesQuery, IReadOnlyList<EmployeeSuggestionDto>>
{
    private const int MaxLimit = 25;
    private const int MinimumTermLength = 2;

    private readonly IUnitOfWork _unitOfWork;

    public SearchEmployeesQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<EmployeeSuggestionDto>> Handle(
        SearchEmployeesQuery request,
        CancellationToken cancellationToken)
    {
        var term = request.Term?.Trim() ?? string.Empty;

        // A single character matches most of the company and is never a real search.
        if (term.Length < MinimumTermLength)
        {
            return [];
        }

        var limit = Math.Clamp(request.Limit, 1, MaxLimit);

        return await _unitOfWork.Repository<Employee, Guid>()
            .GetAllQ()
            .Where(employee => employee.Is_Active
                && (EF.Functions.Like(employee.Display_Name, $"{term}%")
                    || EF.Functions.Like(employee.Display_Name, $"% {term}%")
                    || (employee.Full_Name_Ar != null && EF.Functions.Like(employee.Full_Name_Ar, $"{term}%"))))
            .OrderBy(employee => employee.Display_Name)
            .Take(limit)
            .Select(employee => new EmployeeSuggestionDto
            {
                Id = employee.Id,
                DisplayName = employee.Display_Name,
                JobTitle = employee.JobTitle == null ? null : employee.JobTitle.Name_En ?? employee.JobTitle.Name_Ar,
                ProfilePictureFileId = employee.Profile_Picture_File_Id,
            })
            .ToListAsync(cancellationToken);
    }
}
