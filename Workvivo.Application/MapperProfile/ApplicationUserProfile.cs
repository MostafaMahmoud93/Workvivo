namespace Workvivo.Application.MapperProfile
{
    public class ApplicationUserProfile : MappingProfileBase
    {
        public ApplicationUserProfile()
        {
            CreateMap<ApplicationUser, UserModel>()
                .ForMember(dest => dest.UserTypeName, opt => opt.MapFrom(src => src.MastarDataUserType.Name))
                ;
            CreateMap<UserGroupsLink, GroupsModel>()
                .ForMember(dest => dest.RoleCode, opt => opt.MapFrom(src => src.Role.Name))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Role.GroupName));
            CreateMap<ApplicationUser, DetailUserModel>()
                .ForMember(dest => dest.GroupsName, opt => opt.MapFrom(src => src.UserRoles))
                .ForMember(dest => dest.Groups, opt => opt.MapFrom(src => src.UserRoles.Select(a => a.Role.Name)));

            CreateMap<ApplicationUser, AddUserModel>().ReverseMap();
            CreateMap<ApplicationUser, EditUserModel>().ReverseMap();
            CreateMap<ApplicationUser, AddUserModel>().ForMember(dest => dest.ProfilePicture, opt => opt.Ignore()).ReverseMap();

        }
    }
}
