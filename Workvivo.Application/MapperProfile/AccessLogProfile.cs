namespace Workvivo.Application.MapperProfile
{
    public class AccessLogProfile : MappingProfileBase
    {
        public AccessLogProfile()
        {
            CreateMap<AccessLog, AccessLogModel>()
                .ForMember(dest => dest.MainModuleName, opt => opt.MapFrom(src => src.MainModule.Name))
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.ApplicationUser.Name))
                .ForMember(dest => dest.ScreenName, opt => opt.MapFrom(src => src.LinkScreenAction.Screen.Name))
                .ForMember(dest => dest.ActionName, opt => opt.MapFrom(src => src.LinkScreenAction.ScreenAction.Name))
                .ReverseMap();
            CreateMap<MainModule, AccessLogDropDwonModel>()
              .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Name));
            CreateMap<Screen, AccessLogDropDwonModel>()
              .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Name));
            CreateMap<ScreenAction, AccessLogDropDwonModel>()
              .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Name));
            CreateMap<Lookup, AccessLogDropDwonModel>()
              .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Name));
        }
    }
}