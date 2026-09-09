using Workvivo.Domain.Entities.BaseEntities;
using Workvivo.Domain.Entities.Identity;

namespace Workvivo.Domain.Entities.Common
{
    public class UsersShortCuts : BaseCommonEntity<int>
    {
        public Guid UserID { get; set; }
        public string Label { get; set; }
        public string Description { get; set; }
        public string? Icon { get; set; }
        public string Page { get; set; }
        public bool IsRouter { get; set; }
        public DateTime InsertionDate { get; set; }
        public virtual ApplicationUser User { get; set; }
        public virtual MasterData? IconMasterData { get; set; }
    }
}
