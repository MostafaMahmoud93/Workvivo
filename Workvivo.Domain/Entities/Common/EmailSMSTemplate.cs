using Workvivo.Domain.Entities.BaseEntities;

namespace Workvivo.Domain.Entities.Common
{
    public class EmailSMSTemplate : BaseCommonEntity<int>
    {
        public Guid? Screen_Id { get; set; }
        public string? HTML_Template { get; set; }
        public string HTML_Template_Defult { get; set; }
        public string Subject { get; set; }
        public string? SMS_Content { get; set; }
        public string? HTML_Template_Variables { get; set; }
        public virtual Screen Screen { get; set; }
        public virtual ICollection<EmailSMSHistory>? EmailSMSHistories { get; set; }
        [NotMapped]
        public string? Subject_Lang
        {
            get
            {
                string? subject = Subject.Split("-") != null ? Subject.Split("-")[Thread.CurrentThread.CurrentCulture.TextInfo.IsRightToLeft ? 1 : 0] : Subject;
                return subject;
            }
            set { }
        }
    }
}
