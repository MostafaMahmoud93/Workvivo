using Workvivo.Domain.Abstractions.Classes;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Entities.BaseEntities;
using Workvivo.Domain.Entities.Communities;
using Workvivo.Domain.Entities.Documents;
using Workvivo.Domain.Entities.Organization;

namespace Workvivo.Domain.Entities.Feed;

/// <summary>
/// Something attached to a post: an uploaded file, or a link with its preview.
///
/// Uploads point at a <see cref="FileAsset"/> rather than repeating the file's
/// metadata, so the same image attached twice is stored once and every file in the
/// system is reachable from one registry for retention and cleanup.
/// </summary>
public class PostAttachment : BaseCommonEntity<Guid>
{
    public Guid Post_Id { get; set; }
    public PostAttachmentType Attachment_Type { get; set; }

    /// <summary>Set for Image, Video and Document. Null for a link.</summary>
    public Guid? File_Id { get; set; }

    /// <summary>Set for a link attachment; the other three leave it null.</summary>
    public string? Link_Url { get; set; }
    public string? Link_Title { get; set; }
    public string? Link_Description { get; set; }

    /// <summary>
    /// Preview image for a link, cached as our own file rather than hot-linked.
    /// Hot-linking would leak every viewer's address to the target site and break the
    /// card the moment that site reorganises its assets.
    /// </summary>
    public Guid? Link_Image_File_Id { get; set; }

    public string? Caption { get; set; }
    public int Sort_Order { get; set; }

    public virtual Post? Post { get; set; }
    public virtual FileAsset? File { get; set; }
}
