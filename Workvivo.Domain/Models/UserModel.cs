namespace Workvivo.Domain.Models
{
    public class UserModel
    {
        public Guid Id { get; set; }
        public string UserName { get; set; }
        public string FullNameAr { get; set; }
        public string FullNameEn { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string UserTypeName { get; set; }
        public string? ProfilePictureURL { get; set; }
        public bool IsAdmin { get; set; }
    }
    public class AddUserModel
    {
        public string UserName { get; set; }
        public string FullNameAr { get; set; }
        public string FullNameEn { get; set; }
        public string Email { get; set; }
        public string? Password { get; set; }
        public string UserType { get; set; }
        public string? MobileNo { get; set; }
        public string? OfficeTelNo { get; set; }
        public List<string>? Groups { get; set; }
        public IFormFile? ProfilePicture { get; set; }
        public bool IsActive { get; set; }
    }
    public class EditUserModel : AddUserModel
    {
        public Guid Id { get; set; }
    }
    public class DetailUserModel
    {
        public Guid Id { get; set; }
        public string UserName { get; set; }
        public string FullNameAr { get; set; }
        public string FullNameEn { get; set; }
        public string Email { get; set; }
        public string UserType { get; set; }
        public string UserTypeName { get; set; }
        public string? MobileNo { get; set; }
        public string? OfficeTelNo { get; set; }
        public IFormFile? ProfilePicture { get; set; }
        public string? ProfilePictureURL { get; set; }
        public string? SigneePictureURL { get; set; }
        public List<GroupsModel>? GroupsName { get; set; }
        public List<string>? Groups { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsActive { get; set; }
    }
    public class GroupsModel
    {
        public string RoleCode { get; set; }
        public string Name { get; set; }
    }
    public class UsersDDLModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
    }
}
