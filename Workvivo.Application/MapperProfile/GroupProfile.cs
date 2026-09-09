namespace Workvivo.Application.MapperProfile
{
    public class GroupProfile : MappingProfileBase
    {
        public GroupProfile()
        {
            CreateMap<UserGroup, GroupModel>();
            CreateMap<UserGroup, AddGroupModel>().ReverseMap().ForMember(src => src.UserType, opt => opt.Ignore());
            CreateMap<UserGroup, EditGroupModel>().ReverseMap().ForMember(src => src.UserType, opt => opt.Ignore());
        }
    }
}
