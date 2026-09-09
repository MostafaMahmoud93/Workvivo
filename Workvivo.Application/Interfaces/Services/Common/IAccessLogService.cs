namespace Workvivo.Application.Interfaces.Services.Common;
public interface IAccessLogService
{
    Task<ServiceResponse<bool>> AddHistoryFile(string permission, string? RequestNo, string? notes, string? domain, string? iPAddress);
    Task<ServiceResponse<CollectionResponse<AccessLogModel>>> GetAccessLogs(AccessLogFilterModel filter);
    Task<ServiceResponse<List<AccessLogDropDwonModel>>> GetScreensDDL(Guid? mainModuleId);
    Task<ServiceResponse<List<AccessLogDropDwonModel>>> GetUsersDDL(string? userType);
    Task<ServiceResponse<List<AccessLogDropDwonModel>>> GetScreenActionsDDL();
    Task<ServiceResponse<List<AccessLogDropDwonModel>>> GetMainModulesDDL();
}
