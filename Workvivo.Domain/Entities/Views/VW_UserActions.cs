namespace Workvivo.Domain.Entities.Views
{
    public class VW_UserActions
    {
        public Guid User_Id { get; set; }
        public string Base_Route { get; set; }
        public Guid Screen_Action_Id { get; set; }
        public string Screen_Description_Ar { get; set; }
        public string Screen_Description_En { get; set; }
        public string Action_Name_Ar { get; set; }
        public string Action_Name_En { get; set; }
        public string Action_Code { get; set; }
        [NotMapped]
        public string? Screen_Description
        {
            get
            {
                return Thread.CurrentThread.CurrentCulture.TextInfo.IsRightToLeft ? Screen_Description_Ar : Screen_Description_En ?? Screen_Description_Ar;
            }
            set { }
        }
        [NotMapped]
        public string? Action_Name
        {
            get
            {
                return Thread.CurrentThread.CurrentCulture.TextInfo.IsRightToLeft ? Action_Name_Ar : Action_Name_En ?? Action_Name_Ar;
            }
            set { }
        }
    }
}
