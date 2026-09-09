namespace Workvivo.Application.Implementation.Common;
public class ScreenService : ServiceBase, IScreenService
{
    private readonly IConfiguration _configuration;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    public ScreenService(IUnitOfWork unitOfWork, IMapper mapper, IConfiguration configuration, IUserAccessor userAccessor) : base(configuration, userAccessor)
    {
        _configuration = configuration;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ServiceResponse<NavigationModel>> GetScreens()
    {
        try
        {
            List<string> dbActionCodes = _unitOfWork.VWUserActions.GetAllQ().Where(q => q.User_Id == UserId && q.Screen_Action_Id == ScreenActionCode.View).Select(a => a.Action_Code).ToList();
            MainModule dbModuleModel =
          await _unitOfWork.MainModuleRepository
              .GetAllQ()
              .Where(m => m.Id == (ApplicationId ?? MainModuleSystem.Managment))
              .Include(m => m.Screens.Where(s =>
                  s.Menu_Or_Not && !s.Is_Branch &&
                  (
                      s.SubScreens.Any(sub => sub.Menu_Or_Not && sub.LinkScreenActions.Any(a => dbActionCodes.Contains(a.Action_Code)))
                      || s.Link != "#" && s.LinkScreenActions.Any(a => dbActionCodes.Contains(a.Action_Code))
                  )
                  || s.No_Login // Include screens where No_Login is true
              )
              .OrderBy(s => s.Order))
              .ThenInclude(s => s.SubScreens
                  .Where(sub => sub.Menu_Or_Not && sub.LinkScreenActions.Any(a => dbActionCodes.Contains(a.Action_Code)))
                  .OrderBy(sub => sub.Order))
              .FirstOrDefaultAsync();

            NavigationModel screens = new NavigationModel()
            {
                Default = _mapper.Map<List<ScreenFullModel>>(dbModuleModel?.Screens?.Where(a => a.Parent_Screen_Id == null)),
                Compact = _mapper.Map<List<ScreenShortModel>>(dbModuleModel?.Screens?.Where(a => a.Parent_Screen_Id == null)),
                Futuristic = _mapper.Map<List<ScreenFullModel>>(dbModuleModel?.Screens?.Where(a => a.Parent_Screen_Id == null)),
                Horizontal = _mapper.Map<List<ScreenFullModel>>(dbModuleModel?.Screens?.Where(a => a.Parent_Screen_Id == null))
            };
            return new ServiceResponse<NavigationModel>() { Success = true, Data = screens, Message = ClutureResource.RetrieveData };
        }
        catch (Exception ex)
        {
            return await LogErrorAsync<NavigationModel>(ex, null, null);
        }
    }
}
