namespace Workvivo.Domain.Entities.Common;
public class UserLoginLog : BaseCommonEntity<Guid>
{
    public Guid UserId { get; set; }
    public DateTime LoginTime { get; set; }
    public string IPAddress { get; set; }
    public bool IsSuccessful { get; set; }
    public virtual ApplicationUser? User { get; set; }
}
