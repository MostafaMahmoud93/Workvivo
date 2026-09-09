namespace Workvivo.Domain.Entities.BaseEntities;
public class FullBaseEntity<U> : BaseCommonEntity<U>
{
    public Guid Created_By { get; set; }
    public DateTime Create_Date { get; set; }
    public Guid? Last_Modified_By { get; set; }
    public DateTime? Last_Modify_Date { get; set; }
}
