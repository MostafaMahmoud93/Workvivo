namespace Workvivo.Domain.Models
{
    public class GroupActionModel
    {
        public Guid ActionId { get; set; }
        public Guid GroupId { get; set; }
        public bool IsAdd { get; set; }
    }

    public class UserActionModel
    {
        public Guid ActionId { get; set; }
        public Guid UserId { get; set; }
        public bool IsAdd { get; set; }
    }


    public class ViewGroupActionModel
    {
        public Guid ActionId { get; set; }
        public string ActionName { get; set; }
        public bool Status { get; set; }
    }
    public class GroupScreenActionsModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }
    }
    public class GroupScreensPermissionModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public int Order { get; set; }
        public List<GroupScreenActionsModel> ScreenActions { get; set; }
    }

    public class UserScreenActionsModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }
    }
    public class UserScreensPermissionModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public int Order { get; set; }
        public List<UserScreenActionsModel> ScreenActions { get; set; }
    }
}
