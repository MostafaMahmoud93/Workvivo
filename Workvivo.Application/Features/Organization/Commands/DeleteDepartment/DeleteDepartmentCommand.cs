using MediatR;
using Microsoft.EntityFrameworkCore;
using Workvivo.Application.Common.Messaging;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Domain.Entities.Organization;
using Workvivo.Domain.Exceptions;

namespace Workvivo.Application.Features.Organization.Commands.DeleteDepartment;

public sealed record DeleteDepartmentCommand(Guid Id) : ICommand;

public sealed class DeleteDepartmentCommandHandler : IRequestHandler<DeleteDepartmentCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;

    public DeleteDepartmentCommandHandler(IUnitOfWork unitOfWork, ICacheService cache)
    {
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    public async Task Handle(DeleteDepartmentCommand request, CancellationToken cancellationToken)
    {
        var repository = _unitOfWork.Repository<Department, Guid>();

        var department = await repository.FindByIDAsync(request.Id)
            ?? throw new NotFoundException(nameof(Department), request.Id);

        // Refused rather than cascaded, in both cases.
        //
        // Soft-deleting a department with people in it would leave those employees
        // pointing at a department that no longer appears anywhere, and they would
        // quietly vanish from every departmental filter and audience rule. Whoever is
        // reorganising has to say where the people and sub-departments go.
        var hasChildren = await repository.GetAllQ()
            .AnyAsync(child => child.Parent_Department_Id == request.Id, cancellationToken);

        if (hasChildren)
        {
            throw new BusinessRuleException(
                "Move or remove the sub-departments first.",
                "organization.department-has-children");
        }

        var employeeCount = await _unitOfWork.Repository<Employee, Guid>()
            .GetAllQ()
            .CountAsync(employee => employee.Department_Id == request.Id, cancellationToken);

        if (employeeCount > 0)
        {
            throw new BusinessRuleException(
                $"{employeeCount} employee(s) are still assigned to this department.",
                "organization.department-has-employees");
        }

        department.Is_Deleted = true;
        department.Is_Active = false;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _cache.RemoveAsync("org:lookups", cancellationToken);
    }
}
