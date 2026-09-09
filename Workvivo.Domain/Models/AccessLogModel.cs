namespace Workvivo.Domain.Models;
public class AccessLogModel
{
    public int Id { get; set; }
    public string? MainModuleName { get; set; }
    public string? ScreenName { get; set; }
    public string? ActionName { get; set; }
    public string? UserName { get; set; }
    public DateTime AccessDate { get; set; }
    public string? RecordNo { get; set; }
    public string? Notes { get; set; }
}
public class AccessLogFilterModel : PaggingModel
{
    public DateTime? AccessDateFrom { get; set; }
    public DateTime? AccessDateTo { get; set; }
    public Guid? ScreenId { get; set; }
    public Guid? MainModuleId { get; set; }
    public Guid? UserId { get; set; }
    public Guid? ActionId { get; set; }
    public string? RequestNo { get; set; }
}
public class AccessLogDropDwonModel
{
    public string Id { get; set; }
    public string Description { get; set; }
}
