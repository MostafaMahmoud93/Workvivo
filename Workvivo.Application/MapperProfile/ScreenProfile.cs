namespace Workvivo.Application.MapperProfile;
public class ScreenProfile : MappingProfileBase
{
    public ScreenProfile()
    {
        CreateMap<Screen, ScreenFullModel>()
           .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Link == "#" ? NavigationTypes.Collapsable : NavigationTypes.Basic))
           .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Name))
           .ForMember(dest => dest.Icon, opt => opt.MapFrom(src => src.Screen_Icon))
            .ForMember(dest => dest.Children, opt => opt.MapFrom(src => src.SubScreens))
            .ForMember(dest => dest.ExternalLink, opt => opt.MapFrom(src => src.Link.Contains("http")))
           ;
        CreateMap<Screen, ScreenShortModel>()
           .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Link == "#" ? NavigationTypes.Collapsable : NavigationTypes.Basic))
           .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Name))
           .ForMember(dest => dest.Tooltip, opt => opt.MapFrom(src => src.Name))
           .ForMember(dest => dest.Icon, opt => opt.MapFrom(src => src.Screen_Icon))
           ;

        CreateMap<LinkScreenAction, GroupScreenActionsModel>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.ScreenAction.Name))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.GroupActions.Any(a => a.Link_Screen_Action_Id == src.Id)))
            .ReverseMap();

        CreateMap<Screen, GroupScreensPermissionModel>()
            .ForMember(dest => dest.ScreenActions, opt => opt.MapFrom(src => src.LinkScreenActions))
            .ReverseMap();

        CreateMap<LinkScreenAction, UserScreenActionsModel>()
          .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.ScreenAction.Name))
          .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.UserActions.Any(a => a.Link_Screen_Action_Id == src.Id)))
          .ReverseMap();

        CreateMap<Screen, UserScreensPermissionModel>()
            .ForMember(dest => dest.ScreenActions, opt => opt.MapFrom(src => src.LinkScreenActions))
            .ReverseMap();
    }
}
