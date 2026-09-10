using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Workvivo.Domain.Abstractions.Classes;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Domain.Entities.Organization;
using Workvivo.Domain.Entities.RealTime;

namespace Workvivo.Application.Features.Notifications.Jobs;

/// <summary>
/// Sends one notification to one person by email.
///
/// Reads the text back out of the database rather than taking it as an argument. The
/// job may run minutes after it was queued and is retried by Hangfire on failure -
/// arguments that carried the body would be a snapshot that could outlive a deleted
/// post, and would put the content into the job store as well as the database.
/// </summary>
public sealed class NotificationEmailJob : INotificationEmailJob
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailSender _sender;
    private readonly NotificationEmailRenderer _renderer;
    private readonly ILogger<NotificationEmailJob> _logger;

    public NotificationEmailJob(
        IUnitOfWork unitOfWork,
        IEmailSender sender,
        NotificationEmailRenderer renderer,
        ILogger<NotificationEmailJob> logger)
    {
        _unitOfWork = unitOfWork;
        _sender = sender;
        _renderer = renderer;
        _logger = logger;
    }

    public async Task SendAsync(Guid notificationId, Guid recipientUserId)
    {
        var notification = await _unitOfWork.Repository<Notification, Guid>()
            .GetAllQ()
            .Where(row => row.Id == notificationId)
            .Select(row => new
            {
                row.Header_Ar,
                row.Header_En,
                row.Content_Ar,
                row.Content_En,
                row.RedirectUrl,
            })
            .FirstOrDefaultAsync();

        if (notification is null)
        {
            // Deleted between queueing and running. Not an error - the right outcome is
            // to send nothing.
            _logger.LogDebug("Notification {NotificationId} no longer exists; nothing to send", notificationId);
            return;
        }

        var recipient = await _unitOfWork.Repository<Employee, Guid>()
            .GetAllQ()
            .Where(employee => employee.User_Id == recipientUserId && employee.Is_Active)
            .Select(employee => new { employee.Email, employee.Preferred_Language })
            .FirstOrDefaultAsync();

        if (recipient is null || string.IsNullOrWhiteSpace(recipient.Email))
        {
            return;
        }

        // The recipient's language, not the sender's and not the server's. A background
        // job has no request culture, so LocalizedText would read whatever the worker
        // thread happens to carry.
        var arabic = string.Equals(recipient.Preferred_Language, "ar", StringComparison.OrdinalIgnoreCase);

        var message = _renderer.Single(
            recipient.Email,
            arabic ? notification.Header_Ar : notification.Header_En,
            arabic ? notification.Content_Ar : notification.Content_En,
            notification.RedirectUrl,
            notificationId.ToString("N"));

        await _sender.SendAsync(message);
    }
}
