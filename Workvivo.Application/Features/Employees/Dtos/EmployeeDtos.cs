namespace Workvivo.Application.Features.Employees.Dtos;

/// <summary>An employee as a card in the directory.</summary>
public sealed class EmployeeListItemDto
{
    public Guid Id { get; init; }
    public required string DisplayName { get; init; }
    public string? FullNameAr { get; init; }
    public required string EmployeeNumber { get; init; }
    public string? JobTitle { get; init; }
    public string? Department { get; init; }
    public string? Location { get; init; }
    public string? Email { get; init; }
    public string? Mobile { get; init; }
    public Guid? ProfilePictureFileId { get; init; }
    public bool IsFollowedByMe { get; init; }
}

/// <summary>
/// A full profile page.
///
/// Note what is not here: date of birth. The panel needs a day and a month to say
/// "it's someone's birthday"; the year is the part that identifies a person and it
/// never leaves the database.
/// </summary>
public sealed class EmployeeProfileDto
{
    public Guid Id { get; init; }
    public required string DisplayName { get; init; }
    public string? FullNameAr { get; init; }
    public required string FirstName { get; init; }
    public string? MiddleName { get; init; }
    public required string LastName { get; init; }
    public required string EmployeeNumber { get; init; }
    public string? Email { get; init; }
    public string? Mobile { get; init; }
    public string? Extension { get; init; }
    public string? Biography { get; init; }

    public Guid? DepartmentId { get; init; }
    public string? DepartmentName { get; init; }
    public Guid? TeamId { get; init; }
    public string? TeamName { get; init; }
    public Guid? LocationId { get; init; }
    public string? LocationName { get; init; }
    public Guid? JobTitleId { get; init; }
    public string? JobTitleName { get; init; }

    public Guid? ProfilePictureFileId { get; init; }
    public Guid? CoverPictureFileId { get; init; }

    public DateOnly? JoiningDate { get; init; }

    /// <summary>Day and month only, and only when the employee has not opted out.</summary>
    public int? BirthDay { get; init; }
    public int? BirthMonth { get; init; }

    public string PreferredLanguage { get; init; } = "en";
    public int FollowersCount { get; init; }
    public int FollowingCount { get; init; }
    public int RecognitionPoints { get; init; }
    public bool IsActive { get; init; }

    public bool IsFollowedByMe { get; init; }
    public bool IsMe { get; init; }

    public ManagerSummaryDto? Manager { get; init; }
    public IReadOnlyList<ManagerSummaryDto> DirectReports { get; init; } = [];
    public IReadOnlyList<SkillDto> Skills { get; init; } = [];
    public IReadOnlyList<string> Interests { get; init; } = [];
}

public sealed class ManagerSummaryDto
{
    public Guid Id { get; init; }
    public required string DisplayName { get; init; }
    public string? JobTitle { get; init; }
    public Guid? ProfilePictureFileId { get; init; }
}

public sealed class SkillDto
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public int EndorsementCount { get; init; }
}

/// <summary>Minimal shape for the mention picker and other type-ahead controls.</summary>
public sealed class EmployeeSuggestionDto
{
    public Guid Id { get; init; }
    public required string DisplayName { get; init; }
    public string? JobTitle { get; init; }
    public Guid? ProfilePictureFileId { get; init; }
}
