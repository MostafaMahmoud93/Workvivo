namespace Workvivo.API.Controllers.Auth;
public class AuthController : ApiControllersBase
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAuthService _authService;
    private readonly IMailService _mailService;
    public AuthController(IAuthService authService, IMailService mailService, IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
        _authService = authService;
        _mailService = mailService;
    }
    [AllowAnonymous]
    [HttpPost]
    [Route(RouteClass.Auth.Login)]
    public async Task<IActionResult> Login(LoginModel model)
    {
        //string applicationId = Request.Headers["Application-Id"].ToString();
        return Ok(await _authService.Token(model, MainModuleSystem.Managment));
    }
    [AllowAnonymous]
    [HttpGet]
    [Route(RouteClass.Auth.Test)]
    public async Task<IActionResult> Test()
    {
        return Ok("Test Helloooooo");
    }
}
