using Workvivo.Application.Bases;
using Workvivo.Application.Interfaces.Bases;

namespace Workvivo.Application.Interfaces.Services.Common
{
    public interface IAuthService : IBaseService
    {
        Task<ServiceResponse<TokenModel>> Token(LoginModel model, Guid applicationId);
        Task<ServiceResponse<bool>> IfUserHasActions(string baseRoute);
    }
}
