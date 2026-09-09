namespace Workvivo.Application.Interfaces.Services.Email
{
    public interface IMailService
    {
        Task<ServiceResponse<bool>> SendEmailAsync(int tempId, string[] content, string[] toEmails, string[]? toCCEmails = null, string[]? toBCCEmails = null, bool isImportant = false, List<IFormFile> Attachments = null);
    }
}
