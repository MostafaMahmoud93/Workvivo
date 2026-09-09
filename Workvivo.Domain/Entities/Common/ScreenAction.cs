using Workvivo.Domain.Entities.BaseEntities;

namespace Workvivo.Domain.Entities.Common
{
    public class ScreenAction : BaseCommonEntity<Guid>
    {
        public ScreenAction()
        {
            LinkScreenActions = new HashSet<LinkScreenAction>();
        }
        public string Action_Name_Ar { get; set; }
        public string Action_Name_En { get; set; }
        public int Order { get; set; }
        public virtual ICollection<LinkScreenAction> LinkScreenActions { get; set; }
        [NotMapped]
        public string? Name
        {
            get
            {
                return Thread.CurrentThread.CurrentCulture.TextInfo.IsRightToLeft ? Action_Name_Ar : Action_Name_En ?? Action_Name_Ar;
            }
            set { }
        }
    }
}
