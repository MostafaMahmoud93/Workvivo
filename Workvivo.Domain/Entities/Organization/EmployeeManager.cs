using Workvivo.Domain.Abstractions.Classes;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Entities.BaseEntities;
using Workvivo.Domain.Entities.Identity;

namespace Workvivo.Domain.Entities.Organization;

/// <summary>
/// Reporting line, dated.
///
/// A row per relationship rather than a Manager_Id column on the employee, for two
/// reasons: matrix organisations give people a line manager and a functional manager
/// at the same time, and "who did this person report to when they wrote that post"
/// is a question the audit trail has to answer after a reorganisation.
/// </summary>
public class EmployeeManager : FullBaseEntity<Guid>
{
    public Guid Employee_Id { get; set; }
    public Guid Manager_Id { get; set; }

    /// <summary>
    /// The line manager, as opposed to a dotted-line or functional one. Exactly one
    /// primary manager may be current per employee.
    /// </summary>
    public bool Is_Primary { get; set; } = true;

    public DateOnly Effective_From { get; set; }

    /// <summary>Null while the relationship is current.</summary>
    public DateOnly? Effective_To { get; set; }

    public virtual Employee? Employee { get; set; }
    public virtual Employee? Manager { get; set; }
}
