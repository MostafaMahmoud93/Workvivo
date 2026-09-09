namespace Workvivo.Application.Implementation.Common;
public class ApplicationUserService : ServiceBase, IApplicationUserService
{
    private readonly IConfiguration _configuration;
    private readonly IFileManager _fileManager;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly UploadPath _uploadPath;
    public ApplicationUserService(UploadPath uploadPath, IUnitOfWork unitOfWork, IFileManager fileManager, IMapper mapper, IConfiguration configuration, IUserAccessor userAccessor) : base(configuration, userAccessor)
    {
        _configuration = configuration;
        _fileManager = fileManager;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _uploadPath = uploadPath;
    }
    public async Task<ServiceResponse<bool>> CreateUser(AddUserModel newUser)
    {
        try
        {
            using (TransactionScope scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                ApplicationUser user = await _unitOfWork.UserManager.FindByNameAsync(newUser.UserName.Trim());
                if (user != null) return new ServiceResponse<bool> { Success = false, Data = false, Message = "UserName Used Before !!!" };
                user = _mapper.Map<ApplicationUser>(newUser);
                //user.Is_Active= true;
                if (newUser.ProfilePicture != null && newUser.ProfilePicture?.Length != 0)
                {
                    user.Profile_PictureURL = await _fileManager.SaveFile(_uploadPath.LocalPath, $"imagesUserProfile/{user.Id}", newUser.ProfilePicture.FileName, newUser.ProfilePicture);
                }
                IdentityResult userIdentityResult = await _unitOfWork.UserManager.CreateAsync(user, newUser.Password);
                if (userIdentityResult.Succeeded)
                {
                    if (newUser.Groups is not null && newUser.Groups.Any())
                    {
                        IdentityResult groupIdentityResult = await _unitOfWork.UserManager.AddToRolesAsync(user, newUser.Groups);
                    }
                }
                if (!userIdentityResult.Succeeded) return new ServiceResponse<bool> { Success = false, Data = false, Message = ClutureResource.FaildSave };
                var rest = await _unitOfWork.SaveChangesAsync();
                scope.Complete();
                return new ServiceResponse<bool> { Success = true, Data = true, Message = ClutureResource.SavedSuccessfully };
            }
        }
        catch (Exception ex)
        {
            return await LogErrorAsync(ex, false, newUser);
        }
    }
    public async Task<ServiceResponse<bool>> UpdateImgeProfileUser(IFormFile? profilePicture)
    {
        try
        {
            if (profilePicture == null) return new ServiceResponse<bool> { Success = false, Data = false, Message = ClutureResource.FaildSave };
            using (TransactionScope scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                ApplicationUser user = await _unitOfWork.UserManager.FindByIdAsync(UserId.ToString());

                if (user == null) return new ServiceResponse<bool> { Success = false, Data = false, Message = ClutureResource.FaildSave };

                if (profilePicture != null && profilePicture?.Length != 0)
                {
                    user.Profile_PictureURL = await _fileManager.SaveFile(_uploadPath.LocalPath, $"imagesUserProfile/{user.Id}", profilePicture.FileName, profilePicture);
                }

                var rest = await _unitOfWork.SaveChangesAsync();
                scope.Complete();
                return new ServiceResponse<bool> { Success = true, Data = true, Message = ClutureResource.SavedSuccessfully };
            }
        }
        catch (Exception ex)
        {
            return await LogErrorAsync(ex, false, profilePicture);
        }
    }
    public async Task<ServiceResponse<CollectionResponse<UserModel>>> GetUsers()
    {
        try
        {
            List<ApplicationUser> users = await _unitOfWork.UserManager.Users.Where(a => !a.Is_Deleted).ToListAsync();

            if (users == null) return new ServiceResponse<CollectionResponse<UserModel>> { Success = false, Data = null, Message = ClutureResource.FailedRetrieveData };

            List<UserModel> usersModel = _mapper.Map<List<UserModel>>(users);

            return new ServiceResponse<CollectionResponse<UserModel>> { Success = true, Data = new CollectionResponse<UserModel>(usersModel.Count(), usersModel), Message = ClutureResource.RetrieveData };
        }
        catch (Exception ex)
        {
            return await LogErrorAsync<CollectionResponse<UserModel>>(ex, null, null);
        }
    }
    public async Task<ServiceResponse<CollectionResponse<UserModel>>> SearchUser(string query)
    {
        try
        {
            List<ApplicationUser> users = await _unitOfWork.UserManager.Users
                .Where(a => !a.Is_Deleted &&
                (a.Full_Name_Ar.Contains(query) || a.Full_Name_En.Contains(query) || a.Email.Contains(query)) || query == null
                ).ToListAsync();

            if (users == null) return new ServiceResponse<CollectionResponse<UserModel>> { Success = false, Data = null, Message = ClutureResource.FailedRetrieveData };

            List<UserModel> usersModel = _mapper.Map<List<UserModel>>(users);

            return new ServiceResponse<CollectionResponse<UserModel>> { Success = true, Data = new CollectionResponse<UserModel>(usersModel.Count(), usersModel), Message = ClutureResource.RetrieveData };
        }
        catch (Exception ex)
        {
            return await LogErrorAsync<CollectionResponse<UserModel>>(ex, null, null);
        }
    }
    public async Task<ServiceResponse<DetailUserModel>> GetUser(string userId)
    {
        try
        {
            ApplicationUser user = await _unitOfWork.UserManager.FindByIdAsync(userId);

            if (user == null) return new ServiceResponse<DetailUserModel> { Success = false, Data = new DetailUserModel() { UserName = "New User", Groups = new List<string>() { } }, Message = ClutureResource.FailedRetrieveData };

            DetailUserModel userModel = _mapper.Map<DetailUserModel>(user);

            return new ServiceResponse<DetailUserModel> { Success = true, Data = userModel, Message = ClutureResource.RetrieveData };
        }
        catch (Exception ex)
        {
            return await LogErrorAsync<DetailUserModel>(ex, null, userId);
        }
    }
    public async Task<ServiceResponse<bool>> EditUser(EditUserModel model)
    {
        try
        {
            using (TransactionScope scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                #region Guard
                if (model == null)
                    return new ServiceResponse<bool>() { Success = false, Data = false, Message = ClutureResource.FaildSave };

                ApplicationUser dbUser = await _unitOfWork.UserManager.FindByIdAsync(model.Id.ToString());
                if (dbUser == null) return new ServiceResponse<bool> { Success = false, Data = false, Message = ClutureResource.FailedRetrieveData };
                #endregion
                await _unitOfWork.UserManager.RemoveFromRolesAsync(dbUser, dbUser.UserRoles.Select(q => q.Role.Name).ToList());
                if (model.Groups is not null && model.Groups.Any())
                {
                    await _unitOfWork.UserManager.AddToRolesAsync(dbUser, model.Groups);
                }
                _mapper.Map(model, dbUser);
                if (model.ProfilePicture != null && model.ProfilePicture?.Length != 0)
                {
                    dbUser.Profile_PictureURL = await _fileManager.SaveFile(_uploadPath.LocalPath, $"imagesUserProfile/{model.Id}", model.ProfilePicture.FileName, model.ProfilePicture);
                }
                var rest = await _unitOfWork.SaveChangesAsync();
                scope.Complete();
                return new ServiceResponse<bool> { Success = true, Data = true, Message = ClutureResource.SavedSuccessfully };
            }
        }
        catch (Exception ex)
        {
            return await LogErrorAsync(ex, false, model);
        }
    }
    public async Task<ServiceResponse<bool>> DeleteUser(string userId)
    {
        try
        {
            ApplicationUser user = await _unitOfWork.UserManager.FindByIdAsync(userId);
            if (user == null) return new ServiceResponse<bool> { Success = false, Data = false, Message = ClutureResource.FailedRetrieveData };
            user.Is_Deleted = true;
            var rest = await _unitOfWork.SaveChangesAsync();
            return new ServiceResponse<bool> { Success = true, Data = true, Message = ClutureResource.RetrieveData };
        }
        catch (Exception ex)
        {
            return await LogErrorAsync(ex, false, userId);
        }
    }
    public async Task<ServiceResponse<DetailUserModel>> GetCurrentUser()
    {
        try
        {
            ApplicationUser user = await _unitOfWork.UserManager.FindByIdAsync(GetUserId().ToString());

            if (user == null) return new ServiceResponse<DetailUserModel> { Success = false, Data = null, Message = ClutureResource.FailedRetrieveData };

            DetailUserModel userModel = _mapper.Map<DetailUserModel>(user);

            return new ServiceResponse<DetailUserModel> { Success = true, Data = userModel, Message = ClutureResource.RetrieveData };
        }
        catch (Exception ex)
        {
            return await LogErrorAsync<DetailUserModel>(ex, null, GetUserId());
        }
    }
    public async Task<ServiceResponse<bool>> EditUserProfilePicture(IFormFile profilePicture)
    {
        try
        {
            bool uploadResult = false;
            ApplicationUser dbUser = await _unitOfWork.UserManager.FindByIdAsync(GetUserId().ToString());
            if (profilePicture != null && profilePicture.Length != 0)
            {
                //string filePathRoot = $"imagesUserProfile/{GetUserId()}";
                //string attachmentPath = Path.Combine(filePathRoot, profilePicture.FileName).Replace('\\', '/');
                //await _fileManager.SaveFile(_uploadPath.ServerPath, filePathRoot, profilePicture.FileName, profilePicture);
                dbUser.Profile_PictureURL = await _fileManager.SaveFile(_uploadPath.LocalPath, $"imagesUserProfile/{GetUserId()}", profilePicture.FileName, profilePicture);
                uploadResult = await _unitOfWork.SaveChangesAsync() > 0;
            }
            return new ServiceResponse<bool> { Success = uploadResult, Data = uploadResult, Message = uploadResult ? ClutureResource.SavedSuccessfully : ClutureResource.ErrorOccurredWhileSending };

        }
        catch (Exception ex)
        {
            return await LogErrorAsync(ex, false, GetUserId());
        }
    }
    public async Task<ServiceResponse<bool>> UpdateImgeSignatureUser(IFormFile signaturePicture)
    {
        try
        {
            bool uploadResult = false;
            ApplicationUser dbUser = await _unitOfWork.UserManager.FindByIdAsync(GetUserId().ToString());
            if (signaturePicture != null && signaturePicture.Length != 0)
            {
                //string filePathRoot = $"imagesUserProfile/{GetUserId()}";
                //string attachmentPath = Path.Combine(filePathRoot, profilePicture.FileName).Replace('\\', '/');
                //await _fileManager.SaveFile(_uploadPath.ServerPath, filePathRoot, profilePicture.FileName, profilePicture);
                dbUser.Signee_PictureURL = await _fileManager.SaveFile(_uploadPath.LocalPath, $"signaturePicture/{GetUserId()}", $"{Guid.NewGuid()}.png", signaturePicture);
                uploadResult = await _unitOfWork.SaveChangesAsync() > 0;
            }
            return new ServiceResponse<bool> { Success = uploadResult, Data = uploadResult, Message = uploadResult ? ClutureResource.SavedSuccessfully : ClutureResource.ErrorOccurredWhileSending };

        }
        catch (Exception ex)
        {
            return await LogErrorAsync(ex, false, GetUserId());
        }
    }
    public async Task<ServiceResponse<List<UsersDDLModel>>> GetSmartApprovalUsersDDL()
    {
        try
        {
            List<UsersDDLModel> certificateTemplates = await _unitOfWork.UserManager.Users.Where(a => a.User_Type == UserTypeEnum.PORTA.ToString()).Select(s => new UsersDDLModel() { Id = s.Id, Name = GetCurrentLanguage() == Language.Arabic ? s.Full_Name_Ar : s.Full_Name_En }).ToListAsync();
            return new ServiceResponse<List<UsersDDLModel>> { Success = true, Data = certificateTemplates, Message = ClutureResource.RetrieveData };
        }
        catch (Exception ex)
        {
            return await LogErrorAsync<List<UsersDDLModel>>(ex, null, null);
        }
    }
}
