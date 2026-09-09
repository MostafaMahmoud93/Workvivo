using Workvivo.Application.Bases;

namespace Workvivo.Application.Interfaces.Services.Common
{
    public interface IMasterDataService
    {
        Task<ServiceResponse<List<MasterDataModel>>> GetMasterDataByCode(string code);
        Task<ServiceResponse<List<MasterDataModel>>> GetUserType();
        Task<ServiceResponse<List<MasterDataModel>>> GetIcons();
    }
}
