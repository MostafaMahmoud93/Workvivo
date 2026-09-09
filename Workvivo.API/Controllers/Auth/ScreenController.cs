namespace Workvivo.API.Controllers.Auth;
public class ScreenController : ApiControllersBase
{
    private readonly IScreenService _screenService;
    public ScreenController(IScreenService screenService)
    {
        _screenService = screenService;
    }
    [AllowAnonymous]
    [HttpGet]
    [Route(RouteClass.Screen.GetScreens)]
    public async Task<IActionResult> GetScreens() =>
        Ok(await _screenService.GetScreens());
}
