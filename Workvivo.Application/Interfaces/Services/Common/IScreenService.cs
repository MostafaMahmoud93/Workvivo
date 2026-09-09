using Workvivo.Application.Bases;
using Workvivo.Application.Interfaces.Bases;

namespace Workvivo.Application.Interfaces.Services.Common
{
    public interface IScreenService : IBaseService
    {
        Task<ServiceResponse<NavigationModel>> GetScreens();
    }
}
