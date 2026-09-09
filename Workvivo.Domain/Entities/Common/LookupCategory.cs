using Workvivo.Domain.Entities.BaseEntities;

namespace Workvivo.Domain.Entities.Common
{
    public class LookupCategory : BaseCommonEntity<int>
    {
        public LookupCategory()
        {
            Lookups = new HashSet<Lookup>();
        }
        public string Description_Ar { get; set; }
        public string Description_En { get; set; }
        public bool Is_Changable_By_User { get; set; }
        public string? Code { get; set; }
        public virtual ICollection<Lookup> Lookups { get; set; }
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
