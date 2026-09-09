using Workvivo.Domain.Abstractions.Classes;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Entities.BaseEntities;
using Workvivo.Domain.Entities.Communities;
using Workvivo.Domain.Entities.Documents;
using Workvivo.Domain.Entities.Organization;

namespace Workvivo.Domain.Entities.Feed;

/// <summary>A colleague named in a comment.</summary>
public class CommentMention : BaseCommonEntity<Guid>
{
    public Guid Comment_Id { get; set; }
    public Guid Mentioned_Employee_Id { get; set; }
    public int? Text_Offset { get; set; }
    public int? Text_Length { get; set; }
    public DateTime Mentioned_At { get; set; }

    public virtual Comment? Comment { get; set; }
    public virtual Employee? MentionedEmployee { get; set; }
}
