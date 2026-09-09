namespace Workvivo.Domain.Models.Email;
public class MailRequestModel
{
    public Guid? EmailHistoryId { get; set; }
    public string[] ToEmails { get; set; }
    public string[] ToCCEmails { get; set; }
    public string[] ToBCCEmails { get; set; }
    public string Subject { get; set; }
    public string Body { get; set; }
    public bool IsImportant { get; set; }
    public List<IFormFile>? Attachments { get; set; }
}
public class MailSettings
{
    public string Mail { get; set; }
    public string DisplayName { get; set; }
    public string Password { get; set; }
    public string Host { get; set; }
    public int Port { get; set; }
    public bool IsAuth { get; set; }
    public bool IsEWS { get; set; }
    public string EWSDomain { get; set; }
    public string EWSMail { get; set; }
    public string EWSPassword { get; set; }
    public string EWSHost { get; set; }
    public int EWSPort { get; set; }
}
