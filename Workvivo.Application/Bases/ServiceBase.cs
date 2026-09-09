namespace Workvivo.Application.Bases;
public class ServiceBase : IBaseService
{
    private readonly IConfiguration _configuration;
    private readonly IUserAccessor _userAccessor;
    public ServiceBase(IConfiguration configuration, IUserAccessor userAccessor)
    {
        _configuration = configuration;
        _userAccessor = userAccessor;
    }
    public Guid? UserId
    {
        get
        {
            if (!string.IsNullOrEmpty(_userAccessor.GetCurrentUserId()))
                return Guid.Parse(_userAccessor.GetCurrentUserId());
            else
                return null;
        }
        set { }
    }
    public Guid? ApplicationId
    {
        get
        {
            if (!string.IsNullOrEmpty(_userAccessor.GetCurrentUserApplicationId()))
                return Guid.Parse(_userAccessor.GetCurrentUserApplicationId());
            else
                return null;
        }
        set { }
    }
    public string? UserTypeCode
    {
        get
        {
            if (!string.IsNullOrEmpty(_userAccessor.GetCurrentUserTypeCode()))
                return _userAccessor.GetCurrentUserTypeCode();
            else
                return null;
        }
        set { }
    }
    public static string GetCurrentLanguage()
    {
        return Thread.CurrentThread.CurrentCulture.ToString();
    }
    public Guid? GetUserId()
    {
        return UserId;
    }
    public async Task<ServiceResponse<T>> LogErrorAsync<T>(Exception ex, T data, object inputs)
    {
        if (!Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), DateTime.Now.ToString(@"yyyy\\MM"))))
            Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), DateTime.Now.ToString(@"yyyy\\MM")));

        string path = Path.Combine(Directory.GetCurrentDirectory(), DateTime.Now.ToString(@"yyyy\\MM\\dd")) + ".log";
        string contents = $@"==={DateTime.Now.ToString("HH:mm:ss")}=======================================================================================
                            {ex.Message}
                            ----------------------------------------
                            {ex.InnerException?.Message}
                            ----------------------------------------
                            {ex.StackTrace}
                            ----------------------------------------
                            {UserId}
                            ----------------------------------------
                            {JsonConvert.SerializeObject(inputs)}
                            ==================================================================================================" + Environment.NewLine;
        await File.AppendAllTextAsync(path, contents);
        return new ServiceResponse<T>() { Success = false, Data = data, Message = ClutureResource.ErrorOccurredWhileSending };
    }
    public async Task<T> CustomLogErrorAsync<T>(Exception ex, T data, object inputs)
    {
        if (!Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), DateTime.Now.ToString(@"yyyy\\MM"))))
            Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), DateTime.Now.ToString(@"yyyy\\MM")));

        string path = Path.Combine(Directory.GetCurrentDirectory(), DateTime.Now.ToString(@"yyyy\\MM\\dd")) + ".log";
        string contents = $@"==={DateTime.Now.ToString("HH:mm:ss")}=======================================================================================
                            {ex.Message}
                            ----------------------------------------
                            {ex.InnerException?.Message}
                            ----------------------------------------
                            {ex.StackTrace}
                            ----------------------------------------
                            {UserId}
                            ----------------------------------------
                            {JsonConvert.SerializeObject(inputs)}
                            ==================================================================================================" + Environment.NewLine;
        await File.AppendAllTextAsync(path, contents);
        return data;
    }
}
