using Workvivo.Domain.Entities.BaseEntities;

namespace Workvivo.Domain.Entities.Common
{
    public class EmailSMSHistory : FullBaseEntity<Guid>
    {
        public int Template_Id { get; set; }
        public string To_Emails { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public bool Priority { get; set; }
        public string? Attachment_Files { get; set; }
        public string? CCEmail { get; set; }
        public string? BCCEmail { get; set; }
        public string Email_Sender_Display { get; set; }
        public bool Is_Sent { get; set; }
        public int? Error_Id { get; set; }
        public DateTime Record_Insertion_Datetime { get; set; }
        public int Resend_Try { get; set; }
        public DateTime Last_Resend_Date { get; set; }
        public virtual EmailSMSTemplate EmailSMSTemplate { get; set; }
    }
}
