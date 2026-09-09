namespace Workvivo.Domain.Entities.Common
{
    public class MasterData : BaseCommonEntity<int>
    {

        public string Category_Name { get; set; }
        public string Master_Data_Code { get; set; }
        public int? Master_Data_Parent_Id { get; set; }
        public string Title_Ar { get; set; }
        public string Title_En { get; set; }
        public int Item_Order { get; set; }
        public bool Is_Active { get; set; }
        //silf join
        public virtual MasterData? MasterDataParent { get; set; }
        public virtual ICollection<MasterData>? MasterDataChilds { get; set; }
        public virtual ICollection<ApplicationUser>? ApplicationUserUserType { get; set; }
        public virtual ICollection<UserGroup>? UserGroups { get; set; }
        public virtual ICollection<UsersShortCuts>? UsersShortCutsIcons { get; set; }
        public virtual ICollection<SysSetting>? SysSettings { get; set; }
        [NotMapped]
        public string? Name
        {
            get
            {
                return Thread.CurrentThread.CurrentCulture.TextInfo.IsRightToLeft ? Title_Ar : Title_En ?? Title_Ar;
            }
            set { }
        }
    }
}
