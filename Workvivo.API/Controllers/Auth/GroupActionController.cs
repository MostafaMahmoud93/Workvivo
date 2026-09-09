using Workvivo.Infrastructure.Seeding;
namespace Workvivo.API.Controllers.Auth;
public class GroupActionController : ApiControllersBase
{
    private readonly IGroupActionService _groupActionService;
    public GroupActionController(IGroupActionService groupActionService)
    {
        _groupActionService = groupActionService;
    }

    [HttpPost]
    [HasPermission(Permissions.Role.Manage)]
    [Route(RouteClass.GroupAction.AddEditGroupAction)]
    public async Task<IActionResult> AddEditGroupAction(GroupActionModel groupActionModel)
    => Ok(await _groupActionService.AddEditGroupAction(groupActionModel));

    [HttpGet]
    [Route(RouteClass.GroupAction.GetGroupActions)]
    public async Task<IActionResult> GetGroupActions(Guid groupId)
    => Ok(await _groupActionService.GetGroupActions(groupId));

    [HttpPost]
    [Route(RouteClass.GroupAction.AddEditUserAction)]
    public async Task<IActionResult> AddEditUserAction(UserActionModel userActionModel)
    => Ok(await _groupActionService.AddEditUserAction(userActionModel));

    [HttpGet]
    [Route(RouteClass.GroupAction.GetUserActions)]
    public async Task<IActionResult> GetUserActions(Guid userId)
    => Ok(await _groupActionService.GetUserActions(userId));
}
