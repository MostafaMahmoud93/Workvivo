namespace Workvivo.Domain.Abstractions.Enums;

/// <summary>
/// Employment state.
///
/// Distinct from <c>Is_Active</c>, which is about whether the account may sign in.
/// Someone on long-term leave is Suspended but still an employee whose profile,
/// posts and org-chart position stay intact.
/// </summary>
public enum EmployeeStatus
{
    Active = 0,
    OnLeave = 1,
    Suspended = 2,
    Offboarding = 3,
    Left = 4,
}
