using Workvivo.Domain.Entities.BaseEntities;

namespace Workvivo.Domain.Entities.Common
{
    public class MainModule : BaseCommonEntity<Guid>
    {
        public MainModule()
        {
            Screens = new HashSet<Screen>();
        }
        public string Description_Ar { get; set; }
        public string Description_En { get; set; }
        public int ApplicationNo { get; set; }
        public virtual ICollection<Screen>? Screens { get; set; }
        public virtual ICollection<AccessLog>? AccessLogs { get; set; }
        [NotMapped]
        public string? Name
        {
            get
            {
                return Thread.CurrentThread.CurrentCulture.TextInfo.IsRightToLeft ? Description_Ar : Description_En ?? Description_Ar;
            }
            set { }
        }
    }
}
