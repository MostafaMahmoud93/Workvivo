namespace Workvivo.Domain.Abstractions.Enums;

/// <summary>
/// The dimension an audience row targets. Shared by posts, events, surveys, polls
/// and documents, so one resolver serves them all.
/// </summary>
public enum AudienceType
{
    /// <summary>Everyone in the organisation. <see cref="Target_Id"/> is null.</summary>
    AllEmployees = 0,
    Department = 1,
    Team = 2,
    Location = 3,
    JobTitle = 4,
    Role = 5,
    Community = 6,
    Employee = 7,
}
