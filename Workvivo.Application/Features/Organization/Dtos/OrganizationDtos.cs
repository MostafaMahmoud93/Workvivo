namespace Workvivo.Application.Features.Organization.Dtos;

/// <summary>A department as a row in an administrative list.</summary>
public sealed class DepartmentDto
{
    public Guid Id { get; init; }
    public required string Code { get; init; }
    public required string Name { get; init; }
    public string? NameAr { get; init; }
    public string? NameEn { get; init; }
    public Guid? ParentDepartmentId { get; init; }
    public string? ParentName { get; init; }
    public Guid? ManagerEmployeeId { get; init; }
    public string? ManagerName { get; init; }
    public int Level { get; init; }
    public int EmployeeCount { get; init; }
    public int TeamCount { get; init; }
    public bool IsActive { get; init; }
}

/// <summary>A department and its children, for the org tree.</summary>
public sealed class DepartmentNodeDto
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Code { get; init; }
    public Guid? ParentDepartmentId { get; init; }
    public Guid? ManagerEmployeeId { get; init; }
    public string? ManagerName { get; init; }
    public int EmployeeCount { get; init; }
    public int Level { get; init; }
    public List<DepartmentNodeDto> Children { get; init; } = [];
}

/// <summary>Name-and-id pair for pickers.</summary>
public sealed class LookupItemDto
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public string? Secondary { get; init; }
}

public sealed class TeamDto
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public Guid DepartmentId { get; init; }
    public string? DepartmentName { get; init; }
    public Guid? LeadEmployeeId { get; init; }
    public string? LeadName { get; init; }
    public int EmployeeCount { get; init; }
    public bool IsActive { get; init; }
}

public sealed class LocationDto
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public string? Country { get; init; }
    public string? City { get; init; }
    public string? TimeZoneId { get; init; }
    public int EmployeeCount { get; init; }
    public bool IsActive { get; init; }
}

public sealed class JobTitleDto
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public string? Grade { get; init; }
    public int EmployeeCount { get; init; }
    public bool IsActive { get; init; }
}
