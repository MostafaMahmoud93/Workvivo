namespace Workvivo.API.Controllers.Auth;
public class SysSettingController : ApiControllersBase
{
    private readonly ISysSettingService _sysSettingService;
    public SysSettingController(ISysSettingService sysSettingService)
    {
        _sysSettingService = sysSettingService;
    }
    [HttpGet]
    [Route(RouteClass.SysSetting.GetSystemStamp)]
    public async Task<IActionResult> GetSystemStamp() =>
        Ok(await _sysSettingService.GetSystemStamp());


    [HttpPost]
    [Route(RouteClass.SysSetting.UpdateSystemStamp)]
    public async Task<IActionResult> UpdateSystemStamp(IFormFile stampPicture) =>
        Ok(await _sysSettingService.UpdateSystemStamp(stampPicture));
}
