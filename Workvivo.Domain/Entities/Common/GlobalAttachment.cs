using Workvivo.Domain.Entities.BaseEntities;
using Workvivo.Domain.Entities.Identity;

namespace Workvivo.Domain.Entities.Common
{
    public class GlobalAttachment : BaseCommonEntity<Guid>
    {
        public int Document_Id { get; set; }
        public DateTime CreatedDate { get; set; }
        public Guid CreatedBy { get; set; }
        public string FileExtension { get; set; }
        public string FileMIME { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public long FileSize { get; set; }
        public virtual ApplicationUser User { get; set; }
    }
}
