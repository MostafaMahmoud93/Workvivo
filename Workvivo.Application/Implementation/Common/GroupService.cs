namespace Workvivo.Application.Implementation.Common;
public class GroupService : ServiceBase, IGroupService
{

    private readonly IConfiguration _configuration;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    public GroupService(IUnitOfWork unitOfWork, IMapper mapper, IConfiguration configuration, IUserAccessor userAccessor) : base(configuration, userAccessor)
    {
        _configuration = configuration;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ServiceResponse<bool>> AddGroup(AddGroupModel newGroup)
    {
        try
        {
            UserGroup userGroups = _mapper.Map<UserGroup>(newGroup);
            userGroups.Is_Deleted = false;
            userGroups.Created_By = UserId.Value;
            userGroups.Create_Date = DateTime.Now;
        regeneratCode:
            string code = GenerateRandom.RandomChar(5);
            if (_unitOfWork.UserGroupManager.Roles.Any(a => a.Name == code))
                goto regeneratCode;

            userGroups.Name = code;

            IdentityResult result = await _unitOfWork.UserGroupManager.CreateAsync(userGroups);

            return new ServiceResponse<bool> { Success = result.Succeeded, Data = true, Message = ClutureResource.SavedSuccessfully };
        }
        catch (Exception ex)
        {
            return await LogErrorAsync(ex, false, null);
        }
    }
    public async Task<ServiceResponse<bool>> EditGroup(EditGroupModel model)
    {
        try
        {
            #region Guard
            if (model == null)
                return new ServiceResponse<bool>() { Success = false, Data = false, Message = ClutureResource.FaildSave };
            UserGroup dbUserGroups = await _unitOfWork.UserGroupManager.FindByIdAsync(model.RoleId.ToString());
            if (dbUserGroups == null) return new ServiceResponse<bool> { Success = false, Data = false, Message = ClutureResource.FailedRetrieveData };
            #endregion
            _mapper.Map(model, dbUserGroups);
            var res = await _unitOfWork.SaveChangesAsync();

            return new ServiceResponse<bool> { Success = true, Data = true, Message = ClutureResource.SavedSuccessfully };
        }
        catch (Exception ex)
        {
            return await LogErrorAsync(ex, false, null);
        }
    }
    public async Task<ServiceResponse<CollectionResponse<GroupModel>>> GetGroups()
    {
        try
        {
            List<UserGroup> roles = await _unitOfWork.UserGroupManager.Roles.Where(a => !a.Is_Deleted).ToListAsync();

            List<GroupModel> rolesModel = roles.Select(q => new GroupModel()
            {
                RoleCode = q.Name,
                RoleId = q.Id,
                NameAr = q.Name_Ar,
                NameEn = q.Name_En,
                UserTypeName = q.UserType.Name,
                UserType = q.User_Type,
                IsActive = q.Is_Active,
                IsDeleted = q.Is_Deleted
            }).ToList();

            return new ServiceResponse<CollectionResponse<GroupModel>> { Success = true, Data = new CollectionResponse<GroupModel>(rolesModel.Count(), rolesModel), Message = "Done" };
        }
        catch (Exception ex)
        {
            return await LogErrorAsync<CollectionResponse<GroupModel>>(ex, null, null);
        }
    }
    public async Task<ServiceResponse<List<GroupDDLModel>>> GetGroupsDDL()
    {
        try
        {
            List<UserGroup> roles = await _unitOfWork.UserGroupManager.Roles.Where(a => !a.Is_Deleted).ToListAsync();

            List<GroupDDLModel> rolesModel = roles.Select(q => new GroupDDLModel()
            {
                RoleCode = q.Name,
                Name = q.GroupName
            }).ToList();

            return new ServiceResponse<List<GroupDDLModel>> { Success = true, Data = rolesModel, Message = "Done" };
        }
        catch (Exception ex)
        {
            return await LogErrorAsync<List<GroupDDLModel>>(ex, null, null);
        }
    }
    public async Task<ServiceResponse<bool>> DeleteGroup(Guid id)
    {
        try
        {
            #region Guard
            UserGroup dbUserGroup = await _unitOfWork.UserGroupManager.FindByIdAsync(id.ToString());
            if (dbUserGroup.UserRoles.Any() || dbUserGroup.RoleActions.Any()) return new ServiceResponse<bool> { Success = false, Data = false, Message = ClutureResource.TheGroupCannotBeDeletedForUseInTheSystem };
            #endregion
            dbUserGroup.Is_Deleted = true;
            var res = await _unitOfWork.SaveChangesAsync();
            if (res > 0)
                return new ServiceResponse<bool>() { Success = true, Data = true, Message = ClutureResource.DeletedSuccessfully };
            else
                return new ServiceResponse<bool>() { Success = false, Data = false, Message = ClutureResource.FaildSave };
        }
        catch (Exception ex)
        {
            return await LogErrorAsync(ex, false, id);
        }
    }
}
