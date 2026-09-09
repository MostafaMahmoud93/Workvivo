namespace Workvivo.Domain.Models
{
    public class ModuleModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public List<ScreenFullModel>? Screens { get; set; }
    }
    public class ScreenShortModel
    {
        public Guid Id { get; set; }
        public Guid? ParentScreenId { get; set; }
        public string Title { get; set; }
        public string Tooltip { get; set; }
        public string Link { get; set; }
        public string Type { get; set; }
        public string Icon { get; set; }
    }
    public class ScreenFullModel
    {
        public Guid Id { get; set; }
        public Guid? ParentScreenId { get; set; }
        public string Title { get; set; }
        public string Link { get; set; }
        public string Type { get; set; }
        public string Icon { get; set; }
        public bool ExternalLink { get; set; }
        public List<ScreenFullModel>? Children { get; set; }
    }
    public class NavigationModel
    {
        public List<ScreenFullModel>? Default { get; set; }
        public List<ScreenShortModel>? Compact { get; set; }
        public List<ScreenFullModel>? Futuristic { get; set; }
        public List<ScreenFullModel>? Horizontal { get; set; }
    }
}
