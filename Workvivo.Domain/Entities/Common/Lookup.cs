using Workvivo.Domain.Entities.BaseEntities;

namespace Workvivo.Domain.Entities.Common
{
    public class Lookup : BaseCommonEntity<int>
    {
        public string Category_Code { get; set; }
        public string Description_Ar { get; set; }
        public string Description_En { get; set; }
        public virtual LookupCategory? LookupCategory { get; set; }

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
