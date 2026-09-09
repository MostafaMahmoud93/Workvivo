namespace Workvivo.Application.Implementation.Common;
public class GroupActionService : ServiceBase, IGroupActionService
{
    private readonly IConfiguration _configuration;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    public GroupActionService(IUnitOfWork unitOfWork, IMapper mapper, IConfiguration configuration, IUserAccessor userAccessor) : base(configuration, userAccessor)
    {
        _configuration = configuration;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ServiceResponse<bool>> AddEditGroupAction(GroupActionModel groupActionModel)
    {
        try
        {
            GroupPermissions groupPermission = await _unitOfWork.GroupPermissionsRepository.FirstOrDefaultAsync(q => q.Group_Id == groupActionModel.GroupId && q.Link_Screen_Action_Id == groupActionModel.ActionId);

            if (groupActionModel.IsAdd && groupPermission != null || !groupActionModel.IsAdd && groupPermission == null) return new ServiceResponse<bool> { Success = false, Message = (groupActionModel.IsAdd && groupPermission != null) == true ? "Already Added !" : "Already Deleted !" };

            if (groupActionModel.IsAdd)
            {
                await _unitOfWork.GroupPermissionsRepository.AddAsync(new GroupPermissions { Link_Screen_Action_Id = groupActionModel.ActionId, Group_Id = groupActionModel.GroupId });
            }
            else
            {
                _unitOfWork.GroupPermissionsRepository.DeleteByEntity(groupPermission);
            }

            int result = await _unitOfWork.SaveChangesAsync();

            return new ServiceResponse<bool> { Success = result > 0, Message = ClutureResource.SavedSuccessfully };
        }
        catch (Exception ex)
        {

            return await LogErrorAsync(ex, false, groupActionModel);
        }
    }

    public async Task<ServiceResponse<CollectionResponse<GroupScreensPermissionModel>>> GetGroupActions(Guid groupId)
    {
        try
        {
            var dbGroup = await _unitOfWork.UserGroupManager.Roles.Where(a => a.Id == groupId).FirstOrDefaultAsync(); // i'm sorry for doing that
            MainModule dbModuleModel = await _unitOfWork.MainModuleRepository.GetAllQ()
               .Include(a => a.Screens.Where(s => s.Link != "#").OrderBy(a => a.Order))
               .ThenInclude(d => d.SubScreens.OrderBy(a => a.Order))
               .ThenInclude(a => a.LinkScreenActions.OrderBy(a => a.ScreenAction.Order)).ThenInclude(a => a.ScreenAction)
               .Include(a => a.Screens).ThenInclude(a => a.LinkScreenActions.OrderBy(a => a.ScreenAction.Order))
               .ThenInclude(a => a.GroupActions.Where(a => a.Group_Id == groupId))
               .FirstOrDefaultAsync(a => a.Id == CountMainModuleSystemID(dbGroup.User_Type));
            List<GroupScreensPermissionModel> screens = _mapper.Map<List<GroupScreensPermissionModel>>(dbModuleModel.Screens);

            return new ServiceResponse<CollectionResponse<GroupScreensPermissionModel>> { Success = true, Data = new CollectionResponse<GroupScreensPermissionModel>(screens.Count(), screens) };

        }
        catch (Exception ex)
        {

            return await LogErrorAsync<CollectionResponse<GroupScreensPermissionModel>>(ex, null, new { groupId });
        }
    }
    public async Task<ServiceResponse<bool>> AddEditUserAction(UserActionModel userActionModel)
    {
        try
        {
            UserPermissions userPermissions = await _unitOfWork.UserScreenActionRepository.FirstOrDefaultAsync(q => q.Link_Screen_Action_Id == userActionModel.ActionId && q.User_Id == userActionModel.UserId);

            if (userActionModel.IsAdd && userPermissions != null || !userActionModel.IsAdd && userPermissions == null) return new ServiceResponse<bool> { Success = false, Message = (userActionModel.IsAdd && userPermissions != null) == true ? "Already Added !" : "Already Deleted !" };

            if (userActionModel.IsAdd)
            {
                await _unitOfWork.UserScreenActionRepository.AddAsync(new UserPermissions { Link_Screen_Action_Id = userActionModel.ActionId, User_Id = userActionModel.UserId });
            }
            else
            {
                _unitOfWork.UserScreenActionRepository.DeleteByEntity(userPermissions);
            }

            int result = await _unitOfWork.SaveChangesAsync();

            return new ServiceResponse<bool> { Success = result > 0, Message = ClutureResource.SavedSuccessfully };
        }
        catch (Exception ex)
        {

            return await LogErrorAsync(ex, false, userActionModel);
        }
    }
    public async Task<ServiceResponse<CollectionResponse<UserScreensPermissionModel>>> GetUserActions(Guid userId)
    {
        try
        {
            var dbUser = await _unitOfWork.UserManager.FindByIdAsync(userId.ToString());  // i'm sorry for doing that
            MainModule dbModuleModel = await _unitOfWork.MainModuleRepository.GetAllQ()
                .Include(a => a.Screens.Where(s => s.Link != "#").OrderBy(a => a.Order))
                .ThenInclude(d => d.SubScreens.OrderBy(a => a.Order))
                .ThenInclude(a => a.LinkScreenActions.OrderBy(a => a.ScreenAction.Order)).ThenInclude(a => a.ScreenAction)
                .Include(a => a.Screens).ThenInclude(a => a.LinkScreenActions.OrderBy(a => a.ScreenAction.Order))
                .ThenInclude(a => a.UserActions.Where(a => a.User_Id == userId))
                .FirstOrDefaultAsync(a => a.Id == CountMainModuleSystemID(dbUser.User_Type));
            List<UserScreensPermissionModel> screens = _mapper.Map<List<UserScreensPermissionModel>>(dbModuleModel.Screens);

            return new ServiceResponse<CollectionResponse<UserScreensPermissionModel>> { Success = true, Data = new CollectionResponse<UserScreensPermissionModel>(screens.Count(), screens) };

        }
        catch (Exception ex)
        {

            return await LogErrorAsync<CollectionResponse<UserScreensPermissionModel>>(ex, null, new { userId });
        }
    }

    private Guid CountMainModuleSystemID(string userType)
    {
        if (userType == UserTypeEnum.MANAG.ToString())
        {
            return MainModuleSystem.Managment;
        }
        else if (userType == UserTypeEnum.PORTA.ToString())
        {
            return MainModuleSystem.Portal;
        }
        else
        {
            return Guid.Empty;
        }
    }
}
