using Workvivo.Application.Bases;
using Workvivo.Application.Interfaces.Bases;

namespace Workvivo.Application.Interfaces.Services.Common
{
    public interface IApplicationUserService : IBaseService
    {
        Task<ServiceResponse<CollectionResponse<UserModel>>> SearchUser(string query);
        Task<ServiceResponse<CollectionResponse<UserModel>>> GetUsers();
        Task<ServiceResponse<DetailUserModel>> GetUser(string userId);
        Task<ServiceResponse<bool>> CreateUser(AddUserModel newUser);
        Task<ServiceResponse<bool>> EditUser(EditUserModel model);
        Task<ServiceResponse<DetailUserModel>> GetCurrentUser();
        Task<ServiceResponse<bool>> DeleteUser(string userId);
        Task<ServiceResponse<bool>> EditUserProfilePicture(IFormFile profilePicture);
        Task<ServiceResponse<bool>> UpdateImgeProfileUser(IFormFile? profilePicture);
        Task<ServiceResponse<bool>> UpdateImgeSignatureUser(IFormFile signaturePicture);
        Task<ServiceResponse<List<UsersDDLModel>>> GetSmartApprovalUsersDDL();
    }
}
