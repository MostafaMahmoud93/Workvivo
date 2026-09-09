using Workvivo.Infrastructure.Configuration.Conventions;

namespace Workvivo.Infrastructure.Configuration;

public class PostConfig : IEntityTypeConfiguration<Post>
{
    public void Configure(EntityTypeBuilder<Post> builder)
    {
        builder.ToTable("Feed_Posts");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title_Ar).HasMaxLength(ColumnLengths.Name);
        builder.Property(x => x.Title_En).HasMaxLength(ColumnLengths.Name);

        // Post bodies are genuinely unbounded and are never indexed directly - search
        // goes through full-text on Content_Text, not through a B-tree.
        builder.Property(x => x.Content_Html);
        builder.Property(x => x.Content_Text);

        builder.Property(x => x.RowVersion).IsRowVersion();

        // Domain events are collected in memory during a use case and dispatched after
        // commit; they are not state and must never reach a column.
        builder.Ignore(x => x.DomainEvents);

        // THE feed index. Every timeline page is a backwards range scan over it, and
        // the column order is the query's: filter on status and soft-delete, then walk
        // the (date, id) keyset cursor. Reversing any pair turns the seek into a scan.
        builder.HasIndex(x => new { x.Status, x.Is_Deleted, x.Published_Date, x.Id })
            .HasDatabaseName("IX_Feed_Posts_Timeline")
            .IsDescending(false, false, true, true)
            .IncludeProperties(x => new
            {
                x.Author_Employee_Id,
                x.Community_Id,
                x.Post_Type,
                x.Is_Pinned,
                x.Is_Official,
                x.Reactions_Count,
                x.Comments_Count,
            });

        // Pinned posts are a separate small query unioned on top of page one. Filtered,
        // so the index holds only the handful of rows that are actually pinned rather
        // than a copy of the whole table.
        builder.HasIndex(x => x.Published_Date)
            .HasDatabaseName("IX_Feed_Posts_Pinned")
            .IsDescending(true)
            .HasFilter("[Is_Pinned] = 1 AND [Is_Deleted] = 0");

        // A community's own timeline.
        builder.HasIndex(x => new { x.Community_Id, x.Status, x.Published_Date })
            .HasDatabaseName("IX_Feed_Posts_Community")
            .IsDescending(false, false, true);

        // "My posts" and "my drafts".
        builder.HasIndex(x => new { x.Author_Employee_Id, x.Status, x.Create_Date })
            .HasDatabaseName("IX_Feed_Posts_Author")
            .IsDescending(false, false, true);

        // The scheduled-publish job's only query. Filtered to the rows it can act on,
        // so a job that runs every minute never scans the post table.
        builder.HasIndex(x => x.Scheduled_Publish_Date)
            .HasDatabaseName("IX_Feed_Posts_Scheduled")
            .HasFilter("[Scheduled_Publish_Date] IS NOT NULL AND [Is_Deleted] = 0");

        builder.HasOne(x => x.Author)
            .WithMany()
            .HasForeignKey(x => x.Author_Employee_Id)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Community)
            .WithMany(x => x.Posts)
            .HasForeignKey(x => x.Community_Id)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class PostAttachmentConfig : IEntityTypeConfiguration<PostAttachment>
{
    public void Configure(EntityTypeBuilder<PostAttachment> builder)
    {
        builder.ToTable("Feed_PostAttachments");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Link_Url).HasMaxLength(ColumnLengths.Url);
        builder.Property(x => x.Link_Title).HasMaxLength(ColumnLengths.Name);
        builder.Property(x => x.Link_Description).HasMaxLength(ColumnLengths.Description);
        builder.Property(x => x.Caption).HasMaxLength(ColumnLengths.Description);

        builder.HasIndex(x => x.Post_Id).HasDatabaseName("IX_Feed_PostAttachments_Post");

        builder.HasOne(x => x.Post)
            .WithMany(x => x.Attachments)
            .HasForeignKey(x => x.Post_Id)
            .OnDelete(DeleteBehavior.Cascade);

        // Files outlive the things that reference them; orphan cleanup is a scheduled
        // job, not a cascade. Deleting a post must not silently destroy an image that
        // another post also uses.
        builder.HasOne(x => x.File)
            .WithMany()
            .HasForeignKey(x => x.File_Id)
            .OnDelete(DeleteBehavior.Restrict);

        // An attachment is a file or a link, never both and never neither.
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Feed_PostAttachments_FileOrLink",
            "([File_Id] IS NOT NULL AND [Link_Url] IS NULL) OR ([File_Id] IS NULL AND [Link_Url] IS NOT NULL)"));
    }
}

public class PostAudienceConfig : IEntityTypeConfiguration<PostAudience>
{
    public void Configure(EntityTypeBuilder<PostAudience> builder)
    {
        AudienceConfiguration.Apply(builder, "Feed_PostAudiences", nameof(PostAudience.Post_Id));

        builder.HasOne(x => x.Post)
            .WithMany(x => x.Audiences)
            .HasForeignKey(x => x.Post_Id)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class PostReactionConfig : IEntityTypeConfiguration<PostReaction>
{
    public void Configure(EntityTypeBuilder<PostReaction> builder)
    {
        builder.ToTable("Feed_PostReactions");
        builder.HasKey(x => x.Id);

        // One reaction per person per post. Without this, a double-tap or a retried
        // request quietly inflates the count and the denormalised total drifts from
        // the rows behind it.
        builder.HasIndex(x => new { x.Post_Id, x.Employee_Id })
            .IsUnique()
            .HasDatabaseName("UX_Feed_PostReactions_Post_Employee");

        // The reaction summary: counts grouped by type for one post.
        builder.HasIndex(x => new { x.Post_Id, x.Reaction_Type })
            .HasDatabaseName("IX_Feed_PostReactions_Post_Type");

        builder.HasOne(x => x.Post)
            .WithMany(x => x.Reactions)
            .HasForeignKey(x => x.Post_Id)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.Employee_Id)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class PostMentionConfig : IEntityTypeConfiguration<PostMention>
{
    public void Configure(EntityTypeBuilder<PostMention> builder)
    {
        builder.ToTable("Feed_PostMentions");
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.Post_Id, x.Mentioned_Employee_Id })
            .IsUnique()
            .HasDatabaseName("UX_Feed_PostMentions");

        // "Mentions of me", newest first.
        builder.HasIndex(x => new { x.Mentioned_Employee_Id, x.Mentioned_At })
            .HasDatabaseName("IX_Feed_PostMentions_Employee")
            .IsDescending(false, true);

        builder.HasOne(x => x.Post)
            .WithMany(x => x.Mentions)
            .HasForeignKey(x => x.Post_Id)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.MentionedEmployee)
            .WithMany()
            .HasForeignKey(x => x.Mentioned_Employee_Id)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class PostViewConfig : IEntityTypeConfiguration<PostView>
{
    public void Configure(EntityTypeBuilder<PostView> builder)
    {
        builder.ToTable("Feed_PostViews");
        builder.HasKey(x => x.Id);

        // One row per person per post, so Views_Count means distinct readers rather
        // than page loads - which is the number "has everyone read the safety notice"
        // actually needs.
        builder.HasIndex(x => new { x.Post_Id, x.Employee_Id })
            .IsUnique()
            .HasDatabaseName("UX_Feed_PostViews");

        builder.HasOne(x => x.Post)
            .WithMany()
            .HasForeignKey(x => x.Post_Id)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.Employee_Id)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class CommentConfig : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.ToTable("Feed_Comments");
        builder.HasKey(x => x.Id);

        builder.Ignore(x => x.DomainEvents);

        // Loading a thread: top-level comments for a post in order, then each one's
        // replies. Both are prefixes of this index.
        builder.HasIndex(x => new { x.Post_Id, x.Parent_Comment_Id, x.Create_Date })
            .HasDatabaseName("IX_Feed_Comments_Thread");

        builder.HasIndex(x => x.Author_Employee_Id).HasDatabaseName("IX_Feed_Comments_Author");

        builder.HasOne(x => x.Post)
            .WithMany(x => x.Comments)
            .HasForeignKey(x => x.Post_Id)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Author)
            .WithMany()
            .HasForeignKey(x => x.Author_Employee_Id)
            .OnDelete(DeleteBehavior.Restrict);

        // Restrict, not Cascade: the parent is in the same table, and SQL Server will
        // not accept a self-referencing cascade. Deleting a parent comment is handled
        // in the domain, which soft-deletes the subtree.
        builder.HasOne(x => x.ParentComment)
            .WithMany(x => x.Replies)
            .HasForeignKey(x => x.Parent_Comment_Id)
            .OnDelete(DeleteBehavior.Restrict);

        // The nesting cap, restated at the storage layer. The domain enforces it for
        // application code; this catches a bad import or a hand-written UPDATE, which
        // is exactly where an unbounded thread would come from.
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Feed_Comments_MaxDepth",
            "[Depth] >= 0 AND [Depth] <= 2"));
    }
}

public class CommentReactionConfig : IEntityTypeConfiguration<CommentReaction>
{
    public void Configure(EntityTypeBuilder<CommentReaction> builder)
    {
        builder.ToTable("Feed_CommentReactions");
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.Comment_Id, x.Employee_Id })
            .IsUnique()
            .HasDatabaseName("UX_Feed_CommentReactions");

        builder.HasOne(x => x.Comment)
            .WithMany(x => x.Reactions)
            .HasForeignKey(x => x.Comment_Id)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.Employee_Id)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class CommentMentionConfig : IEntityTypeConfiguration<CommentMention>
{
    public void Configure(EntityTypeBuilder<CommentMention> builder)
    {
        builder.ToTable("Feed_CommentMentions");
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.Comment_Id, x.Mentioned_Employee_Id })
            .IsUnique()
            .HasDatabaseName("UX_Feed_CommentMentions");

        builder.HasIndex(x => new { x.Mentioned_Employee_Id, x.Mentioned_At })
            .HasDatabaseName("IX_Feed_CommentMentions_Employee")
            .IsDescending(false, true);

        builder.HasOne(x => x.Comment)
            .WithMany(x => x.Mentions)
            .HasForeignKey(x => x.Comment_Id)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.MentionedEmployee)
            .WithMany()
            .HasForeignKey(x => x.Mentioned_Employee_Id)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
