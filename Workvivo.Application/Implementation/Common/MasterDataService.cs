namespace Workvivo.Application.Implementation.Common;
public class MasterDataService : ServiceBase, IMasterDataService
{
    private readonly IConfiguration _configuration;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    public MasterDataService(IUnitOfWork unitOfWork, IMapper mapper, IConfiguration configuration, IUserAccessor userAccessor) : base(configuration, userAccessor)
    {
        _configuration = configuration;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ServiceResponse<List<MasterDataModel>>> GetUserType()
    {
        try
        {
            MasterData dbMasterData = await _unitOfWork.MasterData.FirstOrDefaultAsync(a => a.Master_Data_Code == MasterDataCode.UserType);
            List<MasterDataModel> masterDataModel = _mapper.Map<List<MasterDataModel>>(dbMasterData.MasterDataChilds.Where(a => !a.Is_Deleted));
            return new ServiceResponse<List<MasterDataModel>>() { Success = true, Data = masterDataModel, Message = ClutureResource.RetrieveData };
        }
        catch (Exception ex)
        {
            return await LogErrorAsync<List<MasterDataModel>>(ex, null, null);
        }
    }
    public async Task<ServiceResponse<List<MasterDataModel>>> GetIcons()
    {
        try
        {
            MasterData dbMasterData = await _unitOfWork.MasterData.FirstOrDefaultAsync(a => a.Master_Data_Code == MasterDataCode.Icon);

            List<MasterDataModel> masterDataModel = _mapper.Map<List<MasterDataModel>>(dbMasterData.MasterDataChilds.Where(a => !a.Is_Deleted));
            return new ServiceResponse<List<MasterDataModel>>() { Success = true, Data = masterDataModel, Message = ClutureResource.RetrieveData };
        }
        catch (Exception ex)
        {
            return await LogErrorAsync<List<MasterDataModel>>(ex, null, null);
        }
    }
    public async Task<ServiceResponse<List<MasterDataModel>>> GetMasterDataByCode(string code)
    {
        try
        {
            MasterData dbMasterData = await _unitOfWork.MasterData.FirstOrDefaultAsync(a => a.Master_Data_Code == code);

            List<MasterDataModel> masterDataModel = _mapper.Map<List<MasterDataModel>>(dbMasterData.MasterDataChilds.Where(a => !a.Is_Deleted));
            return new ServiceResponse<List<MasterDataModel>>() { Success = true, Data = masterDataModel, Message = ClutureResource.RetrieveData };
        }
        catch (Exception ex)
        {
            return await LogErrorAsync<List<MasterDataModel>>(ex, null, null);
        }
    }
}
