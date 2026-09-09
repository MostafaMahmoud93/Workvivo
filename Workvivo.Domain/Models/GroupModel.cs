namespace Workvivo.Domain.Models;

public class GroupModel
{
    public Guid RoleId { get; set; }
    public string RoleCode { get; set; }
    public string NameAr { get; set; }
    public string NameEn { get; set; }
    public string UserType { get; set; }
    public string UserTypeName { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
}
public class GroupDDLModel
{
    public string RoleCode { get; set; }
    public string Name { get; set; }
    public string UserType { get; set; }
}
public class AddGroupModel
{
    public string NameAr { get; set; }
    public string NameEn { get; set; }
    public string UserType { get; set; }
    public bool IsActive { get; set; }
}
public class EditGroupModel
{
    public Guid RoleId { get; set; }
    public string NameAr { get; set; }
    public string NameEn { get; set; }
    public string UserType { get; set; }
    public bool IsActive { get; set; }
}
