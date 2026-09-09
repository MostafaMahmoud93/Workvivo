using Workvivo.Domain.Abstractions.Classes;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Entities.BaseEntities;
using Workvivo.Domain.Entities.Documents;
using Workvivo.Domain.Entities.Feed;
using Workvivo.Domain.Entities.Organization;

namespace Workvivo.Domain.Entities.Recognition;

/// <summary>
/// One colleague recognising another.
/// </summary>
public class Recognition : AuditableEntity<Guid>
{
    public Guid Sender_Employee_Id { get; set; }
    public Guid Recipient_Employee_Id { get; set; }
    public Guid Recognition_Type_Id { get; set; }

    public string Message { get; set; } = string.Empty;
    public int Points { get; set; }
    public RecognitionVisibility Visibility { get; set; } = RecognitionVisibility.Public;

    /// <summary>
    /// The feed post this recognition generated, when it is public. Null for a private
    /// one, which never reaches the timeline.
    /// </summary>
    public Guid? Post_Id { get; set; }

    public DateTime Recognised_On { get; set; }

    public virtual Employee? Sender { get; set; }
    public virtual Employee? Recipient { get; set; }
    public virtual RecognitionType? RecognitionType { get; set; }
    public virtual Post? Post { get; set; }

    /// <summary>
    /// Recognising yourself is not recognition. Enforced here as well as by a database
    /// check constraint, so neither a new code path nor a direct SQL insert can produce
    /// one and quietly inflate a leaderboard.
    /// </summary>
    public static void EnsureNotSelfRecognition(Guid senderEmployeeId, Guid recipientEmployeeId)
    {
        if (senderEmployeeId == recipientEmployeeId)
        {
            throw new InvalidOperationException("An employee cannot recognise themselves.");
        }
    }
}
