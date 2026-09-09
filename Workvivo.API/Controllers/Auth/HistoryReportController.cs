using Workvivo.Infrastructure.Seeding;
namespace Workvivo.API.Controllers.Auth;
public class HistoryReportController : ApiControllersBase
{
    private readonly IAccessLogService _accessLogService;
    public HistoryReportController(IAccessLogService accessLogService)
    {
        _accessLogService = accessLogService;
    }
    [HttpPost]
    [HasPermission(Permissions.AuditLog.View)]
    [Route(RouteClass.HistoryReport.GetHistoryReport)]
    public async Task<IActionResult> GetHistoryReport(AccessLogFilterModel filter) => Ok(await _accessLogService.GetAccessLogs(filter));
    [HttpGet]
    [Route(RouteClass.HistoryReport.GetMainModulesDDL)]
    public async Task<IActionResult> GetMainModulesDDL() => Ok(await _accessLogService.GetMainModulesDDL());
    [HttpGet]
    [Route(RouteClass.HistoryReport.GetScreensDDL)]
    public async Task<IActionResult> GetScreensDDL(Guid? mainModuleId) => Ok(await _accessLogService.GetScreensDDL(mainModuleId));
    [HttpGet]
    [Route(RouteClass.HistoryReport.GetScreenActionsDDL)]
    public async Task<IActionResult> GetScreenActionsDDL() => Ok(await _accessLogService.GetScreenActionsDDL());
    [HttpGet]
    [Route(RouteClass.HistoryReport.GetUsersDDL)]
    public async Task<IActionResult> GetUsersDDL(string? userType) => Ok(await _accessLogService.GetUsersDDL(userType));
}
