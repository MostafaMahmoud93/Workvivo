namespace Workvivo.Domain.Entities.Common;
public class AccessLog : BaseCommonEntity<int>
{
    public Guid? Main_Module_Id { get; set; }
    public string? Action_Code { get; set; }
    public Guid? User_Id { get; set; }
    public DateTime Access_Date { get; set; }
    public string? RecordNo { get; set; }
    public string? Domain_Controller_User { get; set; }
    public string? IPAddress { get; set; }
    public string? Notes { get; set; }
    public virtual MainModule? MainModule { get; set; }
    public virtual ApplicationUser? ApplicationUser { get; set; }
    public virtual LinkScreenAction? LinkScreenAction { get; set; }
}
