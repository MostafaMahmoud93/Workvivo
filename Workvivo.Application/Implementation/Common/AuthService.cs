using Workvivo.Domain.Entities.Views;

namespace Workvivo.Application.Implementation.Common;
public class AuthService : ServiceBase, IAuthService
{
    private readonly string _publicKey = "A12#sD89*&fd^45Gkl@6789^DE34fdsQw87@32hD12%9fDs!@#fd4dsT3";
    private static string _encryptionKey = "Fi@d0Cer2Ti0Fi24";
    private readonly IConfiguration _configuration;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    public AuthService( IUnitOfWork unitOfWork, IMapper mapper, IConfiguration configuration, IUserAccessor userAccessor) : base(configuration, userAccessor)
    {
        _configuration = configuration;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }
    public async Task<ServiceResponse<TokenModel>> Token(LoginModel model, Guid applicationId)
    {
        try
        {
            ApplicationUser user = await _unitOfWork.UserManager.FindByNameAsync(model.UserName.Trim());
            if (user != null)
            {
                var thirtyMinutesAgo = DateTime.UtcNow.AddMinutes(-30);
                int failedLoginAttempts = await _unitOfWork.UserLoginLogRepository.GetAllQ().CountAsync(log => log.UserId == user.Id && log.LoginTime >= thirtyMinutesAgo && !log.IsSuccessful);

                if (failedLoginAttempts >= 5)
                {
                    return new ServiceResponse<TokenModel> { Success = false, Data = null, Message = string.Format(ClutureResource.FailedLoginPleaseTryAgainAfterMinutes, 30) };
                }
            }
            SignInResult result = await _unitOfWork.SignInManager.PasswordSignInAsync(model.UserName.Trim(), model.Password.Trim(), true, lockoutOnFailure: true);
            if (user != null)
            {
                // Create a new login log entry
                UserLoginLog loginLog = new UserLoginLog
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    LoginTime = DateTime.UtcNow,
                    IPAddress = GetIpAddress(), // Implement this method to capture the IP address
                    IsSuccessful = result.Succeeded,
                    Is_Deleted = false
                };
                // Log the login attempt
                await _unitOfWork.UserLoginLogRepository.AddAsync(loginLog);
                await _unitOfWork.SaveChangesAsync();
            }

            if (user == null || !result.Succeeded) return new ServiceResponse<TokenModel> { Success = false, Data = null, Message = ClutureResource.NotAuthorized };
            if (!user.Is_Active) return new ServiceResponse<TokenModel> { Success = false, Data = null, Message = ClutureResource.InActiveAccount };
            TokenModel token = await GenerateToken(user, model.Password.Trim(), _publicKey, "SGS.com", "SGS.com", applicationId, 1440);

            if (token != null)
            {
                //if (applicationId == (user.User_Type == UserTypeEnum.MANAG.ToString() ? MainModuleSystem.Managment : MainModuleSystem.Portal))
                if (true)
                {
                    return new ServiceResponse<TokenModel> { Success = true, Data = token, Message = ClutureResource.SenedSuccessfully };
                }
                else
                {
                    return new ServiceResponse<TokenModel>
                    {
                        Success = false,
                        Data = null,
                        Message = applicationId == MainModuleSystem.Portal ? ClutureResource.YouDoNotHavePermissionAsAdministrationUserToAccessPortal : ClutureResource.YouDoNotHavePermissionAsManagmentUserToEnterAdministration
                    };
                }
            }
            return new ServiceResponse<TokenModel> { Success = false, Data = null, Message = ClutureResource.NotAuthorized };
        }
        catch (Exception ex)
        {
            return await LogErrorAsync<TokenModel>(ex, null, model);
        }
    }
    public async Task<ServiceResponse<bool>> IfUserHasActions(string baseRoute)
    {
        try
        {
            List<VW_UserActions> userActions = await _unitOfWork.VWUserActions.GetAllAsync(q => q.User_Id == UserId);
            baseRoute = string.Concat(ApplicationId == MainModuleSystem.Portal ? "portal" : "managment", baseRoute);
            return new ServiceResponse<bool>() { Success = true, Data = !userActions.Any(q => baseRoute.ToLower().Contains(q.Base_Route.ToLower())), Message = ClutureResource.YouDoNotHavePermissionForScreen };
        }
        catch (Exception ex)
        {
            return await LogErrorAsync(ex, false, null);
        }
    }
    #region private help function
    private async Task<TokenModel> GenerateToken(ApplicationUser user, string? password, string topSecretKey, string issuer, string audience, Guid applicationId, int ExpireTime = 0)
    {
        if (user != null)
        {
            var claims = new[]{
                new Claim(JwtRegisteredClaimNames.NameId, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.UserName),
                new Claim("DepartmentNo", user.DepartmentNo.ToString()),
                new Claim("ApplicationId", applicationId.ToString()),
                new Claim("UserTypeCode", user.User_Type),
                new Claim("IsAdmin", user.Is_Admin.ToString()),
                new Claim("Exp", ExpireTime.ToString()),
            };
            var superSecretPassword = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(topSecretKey));
            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                expires: DateTime.Now.AddMinutes(ExpireTime),
                claims: claims,
                signingCredentials: new SigningCredentials(superSecretPassword, SecurityAlgorithms.HmacSha256)
            );
            string[] userActions = await _unitOfWork.VWUserActions.GetAllQ().Where(q => q.User_Id == user.Id).Select(a => a.Action_Code).ToArrayAsync();
            return new TokenModel
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                Expiration = token.ValidTo,
                UserId = user.Id,
                IsAdmin = user.Is_Admin,
                UserType = user.User_Type,
                UserActions = userActions
            };
        }
        return null;
    }
    private string GetIpAddress()
    {
        // Implement logic to get IP address
        return "0.0.0.0"; // Example
    }
    #endregion
}
