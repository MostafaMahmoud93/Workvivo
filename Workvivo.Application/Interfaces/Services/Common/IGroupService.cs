using Workvivo.Application.Bases;
using Workvivo.Application.Interfaces.Bases;

namespace Workvivo.Application.Interfaces.Services.Common
{
    public interface IGroupService : IBaseService
    {
        Task<ServiceResponse<CollectionResponse<GroupModel>>> GetGroups();
        Task<ServiceResponse<List<GroupDDLModel>>> GetGroupsDDL();
        Task<ServiceResponse<bool>> AddGroup(AddGroupModel newRole);
        Task<ServiceResponse<bool>> EditGroup(EditGroupModel model);
        Task<ServiceResponse<bool>> DeleteGroup(Guid id);
    }
}
