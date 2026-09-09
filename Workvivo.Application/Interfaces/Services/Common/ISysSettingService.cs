using Workvivo.Application.Bases;

namespace Workvivo.Application.Interfaces.Services.Common
{
    public interface ISysSettingService
    {
        Task<ServiceResponse<bool>> UpdateSystemStamp(IFormFile systemStamp);
        Task<ServiceResponse<string>> GetSystemStamp();
    }
}
