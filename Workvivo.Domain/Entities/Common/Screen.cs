using Workvivo.Domain.Entities.BaseEntities;

namespace Workvivo.Domain.Entities.Common
{
    public class Screen : BaseCommonEntity<Guid>
    {
        public Screen()
        {
            LinkScreenActions = new HashSet<LinkScreenAction>();
        }
        public Guid Main_Module_Id { get; set; }
        public string Screen_Description_Ar { get; set; }
        public string Screen_Description_En { get; set; }
        public Guid? Parent_Screen_Id { get; set; }
        public string Link { get; set; }
        public bool Is_Branch { get; set; }
        public int Order { get; set; }
        public bool Menu_Or_Not { get; set; }
        public bool No_Login { get; set; }
        public string Screen_Icon { get; set; }
        public virtual ICollection<LinkScreenAction> LinkScreenActions { get; set; }
        public virtual ICollection<EmailSMSTemplate> EmailSMSTemplates { get; set; }
        //silf join
        public virtual Screen? ParentScreen { get; set; }
        public virtual ICollection<Screen>? SubScreens { get; set; }
        public virtual MainModule? MainModule { get; set; }
        [NotMapped]
        public string? Name
        {
            get
            {
                return Thread.CurrentThread.CurrentCulture.TextInfo.IsRightToLeft ? Screen_Description_Ar : Screen_Description_En ?? Screen_Description_Ar;
            }
            set { }
        }
    }
}
