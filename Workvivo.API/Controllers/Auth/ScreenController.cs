namespace Workvivo.API.Controllers.Auth;
public class ScreenController : ApiControllersBase
{
    private readonly IScreenService _screenService;
    public ScreenController(IScreenService screenService)
    {
        _screenService = screenService;
    }
    // Was [AllowAnonymous]: this builds the navigation menu from the caller's own
    // permissions, so without an identity it cannot produce a correct answer, and it
    // exposed the administrative screen structure to signed-out visitors.
    [HttpGet]
    [Route(RouteClass.Screen.GetScreens)]
    public async Task<IActionResult> GetScreens() =>
        Ok(await _screenService.GetScreens());
}
