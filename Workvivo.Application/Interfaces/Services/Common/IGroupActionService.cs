using Workvivo.Application.Bases;
using Workvivo.Application.Interfaces.Bases;

namespace Workvivo.Application.Interfaces.Services.Common
{
    public interface IGroupActionService : IBaseService
    {
        Task<ServiceResponse<bool>> AddEditGroupAction(GroupActionModel groupActionModel);
        Task<ServiceResponse<bool>> AddEditUserAction(UserActionModel userActionModel);
        Task<ServiceResponse<CollectionResponse<GroupScreensPermissionModel>>> GetGroupActions(Guid groupId);
        Task<ServiceResponse<CollectionResponse<UserScreensPermissionModel>>> GetUserActions(Guid userId);
    }
}
