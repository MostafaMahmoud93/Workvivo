using Workvivo.Infrastructure.Seeding;
namespace Workvivo.API.Controllers.Auth;
public class UserController : ApiControllersBase
{
    private readonly IApplicationUserService _applicationUserService;
    public UserController(IApplicationUserService applicationUserService)
    {
        _applicationUserService = applicationUserService;
    }
    [HttpPost]
    [HasPermission(Permissions.Employee.Create)]
    [Route(RouteClass.User.CreateUser)]
    public async Task<IActionResult> CreateUser([FromForm] AddUserModel newUser) =>
        Ok(await _applicationUserService.CreateUser(newUser));

    [HttpPost]
    [HasPermission(Permissions.Employee.Edit)]
    [Route(RouteClass.User.EditUser)]
    public async Task<IActionResult> EditUser([FromForm] EditUserModel newUser) =>
        Ok(await _applicationUserService.EditUser(newUser));

    [HttpPost]
    [Route(RouteClass.User.UpdateImgeProfileUser)]
    public async Task<IActionResult> UpdateImgeProfileUser(IFormFile? ProfilePicture) =>
        Ok(await _applicationUserService.UpdateImgeProfileUser(ProfilePicture));

    [HttpGet]
    [HasPermission(Permissions.Employee.View)]
    [Route(RouteClass.User.GetUsers)]
    public async Task<IActionResult> GetUsers() =>
        Ok(await _applicationUserService.GetUsers());

    [HttpGet]
    [Route(RouteClass.User.GetUser)]
    public async Task<IActionResult> GetUser(Guid userId) =>
        Ok((await _applicationUserService.GetUser(userId.ToString())).Data);

    [HttpGet]
    [Route(RouteClass.User.SearchUser)]
    public async Task<IActionResult> SearchUser(string? query) =>
        Ok(await _applicationUserService.SearchUser(query));

    [HttpGet]
    [Route(RouteClass.User.GetCurrentUser)]
    public async Task<IActionResult> GetCurrentUser() =>
        Ok((await _applicationUserService.GetCurrentUser()).Data);

    [HttpPost]
    [HasPermission(Permissions.Employee.Delete)]
    [Route(RouteClass.User.DeleteUser)]
    public async Task<IActionResult> DeleteUser(Guid userId) =>
        Ok((await _applicationUserService.DeleteUser(userId.ToString())).Data);

    [HttpPost]
    [Route(RouteClass.User.EditUserProfilePicture)]
    public async Task<IActionResult> EditUserProfilePicture(IFormFile ProfilePicture) =>
        Ok(await _applicationUserService.EditUserProfilePicture(ProfilePicture));

    [HttpPost]
    [Route(RouteClass.User.UpdateImgeSignatureUser)]
    public async Task<IActionResult> UpdateImgeSignatureUser(IFormFile signaturePicture) =>
        Ok(await _applicationUserService.UpdateImgeSignatureUser(signaturePicture));

    [HttpGet]
    [Route(RouteClass.User.GetSmartApprovalUsersDDL)]
    public async Task<IActionResult> GetSmartApprovalUsersDDL() =>
        Ok(await _applicationUserService.GetSmartApprovalUsersDDL());

}
