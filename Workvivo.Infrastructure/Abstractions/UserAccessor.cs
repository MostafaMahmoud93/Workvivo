namespace Workvivo.Infrastructure.Abstractions;
public class UserAccessor : IUserAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    public UserAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }
    public int Lang { get; } = Thread.CurrentThread.CurrentCulture.TextInfo.IsRightToLeft ? 0 : 1;// arabic 0 English 1
    public int GetLang() => Thread.CurrentThread.CurrentCulture.TextInfo.IsRightToLeft ? 0 : 1;
    public string? GetCurrentUserId() => _httpContextAccessor.HttpContext?.User?.Claims?.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;
    public string? GetCurrentUserApplicationId() => _httpContextAccessor.HttpContext?.User?.Claims?.FirstOrDefault(x => x.Type == "ApplicationId")?.Value;
    public string? GetCurrentUserTypeCode() => _httpContextAccessor.HttpContext?.User?.Claims?.FirstOrDefault(x => x.Type == "UserTypeCode")?.Value;
}
