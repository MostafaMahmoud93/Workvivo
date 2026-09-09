namespace Workvivo.Application.Implementation.Common;
public class SysSettingService : ServiceBase, ISysSettingService
{
    private readonly IConfiguration _configuration;
    private readonly IFileManager _fileManager;
    private readonly IUnitOfWork _unitOfWork;
    private readonly UploadPath _uploadPath;
    private readonly IMapper _mapper;
    public SysSettingService(UploadPath uploadPath, IUnitOfWork unitOfWork, IFileManager fileManager, IMapper mapper, IConfiguration configuration, IUserAccessor userAccessor) : base(configuration, userAccessor)
    {
        _configuration = configuration;
        _fileManager = fileManager;
        _uploadPath = uploadPath;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }
    public async Task<ServiceResponse<string>> GetSystemStamp()
    {
        try
        {
            bool uploadResult = false;
            string dbSystemStamp = await _unitOfWork.SysSettingRepository.SelectFirstPropertyValue<string, string>(a => a.SysSettingCode == SysSettingCode.SystemStamp, a => a.SysSettingValue);
            return new ServiceResponse<string> { Success = uploadResult, Data = dbSystemStamp, Message = ClutureResource.RetrieveData };

        }
        catch (Exception ex)
        {
            return await LogErrorAsync<string>(ex, null, null);
        }
    }
    public async Task<ServiceResponse<bool>> UpdateSystemStamp(IFormFile systemStamp)
    {
        try
        {
            bool uploadResult = false;
            SysSetting dbSystemStamp = await _unitOfWork.SysSettingRepository.FirstOrDefaultAsync(a => a.SysSettingCode == SysSettingCode.SystemStamp);
            if (systemStamp != null && systemStamp.Length != 0)
            {
                dbSystemStamp.SysSettingValue = await _fileManager.SaveFile(_uploadPath.LocalPath, $"systemStampPicture/{GetUserId()}", $"{Guid.NewGuid()}.png", systemStamp);
                uploadResult = await _unitOfWork.SaveChangesAsync() > 0;
            }
            return new ServiceResponse<bool> { Success = uploadResult, Data = uploadResult, Message = uploadResult ? ClutureResource.SavedSuccessfully : ClutureResource.ErrorOccurredWhileSending };

        }
        catch (Exception ex)
        {
            return await LogErrorAsync(ex, false, GetUserId());
        }
    }
}
