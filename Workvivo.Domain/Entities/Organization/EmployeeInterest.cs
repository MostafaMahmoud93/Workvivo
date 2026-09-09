using Workvivo.Domain.Abstractions.Classes;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Entities.BaseEntities;
using Workvivo.Domain.Entities.Identity;

namespace Workvivo.Domain.Entities.Organization;

/// <summary>An interest listed on an employee's profile.</summary>
public class EmployeeInterest : BaseCommonEntity<Guid>
{
    public Guid Employee_Id { get; set; }
    public Guid Interest_Id { get; set; }
    public DateTime Added_At { get; set; }

    public virtual Employee? Employee { get; set; }
    public virtual Interest? Interest { get; set; }
}
