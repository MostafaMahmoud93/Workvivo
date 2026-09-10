using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Workvivo.Application.Common.Messaging;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Domain.Entities.Organization;
using Workvivo.Domain.Exceptions;

namespace Workvivo.Application.Features.Organization.Commands.SaveDepartment;

/// <summary>Creates a department, or updates one when <see cref="Id"/> is supplied.</summary>
public sealed record SaveDepartmentCommand(
    Guid? Id,
    Guid OrganizationId,
    Guid? ParentDepartmentId,
    string Code,
    string NameAr,
    string? NameEn,
    string? DescriptionAr,
    string? DescriptionEn,
    Guid? ManagerEmployeeId,
    int SortOrder,
    bool IsActive) : ICommand<Guid>;

public sealed class SaveDepartmentCommandValidator : AbstractValidator<SaveDepartmentCommand>
{
    public SaveDepartmentCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(100);
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(200);
        RuleFor(x => x.NameEn).MaximumLength(200);
        RuleFor(x => x.DescriptionAr).MaximumLength(1000);
        RuleFor(x => x.DescriptionEn).MaximumLength(1000);

        // Guarded by When, because without it a new top-level department - no id, no
        // parent - compares null against null, the rule reads that as a violation, and
        // creating a root department becomes impossible.
        RuleFor(x => x.ParentDepartmentId)
            .Must((command, parentId) => parentId != command.Id)
            .WithMessage("A department cannot be its own parent.")
            .When(x => x.Id.HasValue && x.ParentDepartmentId.HasValue);
    }
}

public sealed class SaveDepartmentCommandHandler : IRequestHandler<SaveDepartmentCommand, Guid>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;

    public SaveDepartmentCommandHandler(IUnitOfWork unitOfWork, ICacheService cache)
    {
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    public async Task<Guid> Handle(SaveDepartmentCommand request, CancellationToken cancellationToken)
    {
        var repository = _unitOfWork.Repository<Department, Guid>();

        var duplicateCode = await repository.GetAllQ()
            .AnyAsync(
                department => department.Code == request.Code && department.Id != request.Id,
                cancellationToken);

        if (duplicateCode)
        {
            throw new ConflictException($"A department with code '{request.Code}' already exists.");
        }

        var parent = await LoadParentAsync(request.ParentDepartmentId, cancellationToken);

        var department = request.Id is { } id
            ? await repository.FindByIDAsync(id) ?? throw new NotFoundException(nameof(Department), id)
            : null;

        if (department is null)
        {
            department = new Department { Id = Guid.NewGuid(), Organization_Id = request.OrganizationId };
            await repository.AddAsync(department);
        }
        else
        {
            EnsureMoveIsLegal(department, parent);
        }

        var previousPath = department.Path;

        department.Parent_Department_Id = parent?.Id;
        department.Code = request.Code.Trim();
        department.Name_Ar = request.NameAr.Trim();
        department.Name_En = request.NameEn?.Trim();
        department.Description_Ar = request.DescriptionAr?.Trim();
        department.Description_En = request.DescriptionEn?.Trim();
        department.Manager_Employee_Id = request.ManagerEmployeeId;
        department.Sort_Order = request.SortOrder;
        department.Is_Active = request.IsActive;
        department.Path = DepartmentPath.Build(parent?.Path, department.Id);
        department.Level = DepartmentPath.LevelOf(department.Path);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Moving a department moves everything under it. The descendants' paths encode
        // their ancestry, so they are stale the moment the parent changes - and every
        // subtree query, audience filter and org chart reads them.
        if (!string.IsNullOrEmpty(previousPath) && previousPath != department.Path)
        {
            await RepointSubtreeAsync(previousPath, department.Path, cancellationToken);
        }

        await _cache.RemoveAsync("org:lookups", cancellationToken);

        return department.Id;
    }

    private async Task<Department?> LoadParentAsync(Guid? parentId, CancellationToken cancellationToken)
    {
        if (parentId is null)
        {
            return null;
        }

        return await _unitOfWork.Repository<Department, Guid>().FindByIDAsync(parentId.Value)
            ?? throw new NotFoundException(nameof(Department), parentId.Value);
    }

    /// <summary>
    /// Refuses a move that would put a department inside its own subtree.
    ///
    /// The result would not be an error, it would be a ring: the org chart recurses
    /// forever, subtree queries return nonsense, and the branch disappears from every
    /// view that walks down from a root - with nothing to point at as the cause.
    /// </summary>
    private static void EnsureMoveIsLegal(Department department, Department? newParent)
    {
        if (DepartmentPath.WouldCreateCycle(department.Path, newParent?.Path))
        {
            throw new BusinessRuleException(
                "A department cannot be moved beneath one of its own sub-departments.",
                "organization.department-cycle");
        }
    }

    /// <summary>
    /// Rewrites the path and level of every descendant after a move.
    ///
    /// Loaded and saved as a set rather than one row at a time: a move is rare, the
    /// subtree is bounded by the size of the organisation, and doing it in one unit of
    /// work means a failure cannot leave half the hierarchy pointing at the old parent.
    /// </summary>
    private async Task RepointSubtreeAsync(
        string previousPath,
        string newPath,
        CancellationToken cancellationToken)
    {
        var descendants = await _unitOfWork.Repository<Department, Guid>()
            .GetAllQ()
            .Where(department => department.Path.StartsWith(previousPath))
            .ToListAsync(cancellationToken);

        foreach (var descendant in descendants)
        {
            descendant.Path = DepartmentPath.Reparent(descendant.Path, previousPath, newPath);
            descendant.Level = DepartmentPath.LevelOf(descendant.Path);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
