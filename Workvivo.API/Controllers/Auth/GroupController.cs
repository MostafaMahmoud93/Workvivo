using Workvivo.Infrastructure.Seeding;
namespace Workvivo.API.Controllers.Auth;
public class GroupController : ApiControllersBase
{
    private readonly IGroupService _groupService;
    public GroupController(IGroupService groupService)
    {
        _groupService = groupService;
    }


    [HttpGet]
    [HasPermission(Permissions.Role.Manage)]
    [Route(RouteClass.Group.GetGroups)]
    public async Task<IActionResult> GetGroups() =>
        Ok(await _groupService.GetGroups());

    [HttpGet]
    [Route(RouteClass.Group.GetGroupsDDL)]
    public async Task<IActionResult> GetGroupsDDL() =>
     Ok(await _groupService.GetGroupsDDL());

    [HttpPost]
    [Route(RouteClass.Group.CreateGroup)]
    public async Task<IActionResult> CreateGroup(AddGroupModel groupModel) =>
        Ok(await _groupService.AddGroup(groupModel));

    [HttpPost]
    [Route(RouteClass.Group.EditGroup)]
    public async Task<IActionResult> EditGroup(EditGroupModel groupModel) =>
        Ok(await _groupService.EditGroup(groupModel));

    [HttpPost]
    [Route(RouteClass.Group.DeleteGroup)]
    public async Task<IActionResult> DeleteGroup(Guid id) =>
        Ok(await _groupService.DeleteGroup(id));


}
