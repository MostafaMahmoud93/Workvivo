using Workvivo.Infrastructure.Configuration.Conventions;

namespace Workvivo.Infrastructure.Configuration;

public class CommunityConfig : IEntityTypeConfiguration<Community>
{
    public void Configure(EntityTypeBuilder<Community> builder)
    {
        builder.ToTable("Comm_Communities");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Slug).IsRequired().HasMaxLength(ColumnLengths.Code);
        builder.Property(x => x.Name_Ar).IsRequired().HasMaxLength(ColumnLengths.Name);
        builder.Property(x => x.Name_En).HasMaxLength(ColumnLengths.Name);
        builder.Property(x => x.Description_Ar).HasMaxLength(ColumnLengths.Description);
        builder.Property(x => x.Description_En).HasMaxLength(ColumnLengths.Description);

        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.Ignore(x => x.DomainEvents);

        builder.HasIndex(x => x.Slug).IsUnique().HasDatabaseName("UX_Comm_Communities_Slug");

        // The discovery page: active communities, most populous first.
        builder.HasIndex(x => new { x.Is_Deleted, x.Is_Active, x.Privacy })
            .HasDatabaseName("IX_Comm_Communities_Discovery")
            .IncludeProperties(x => new { x.Name_Ar, x.Name_En, x.Members_Count, x.Is_Featured });

        builder.HasOne(x => x.Owner)
            .WithMany()
            .HasForeignKey(x => x.Owner_Employee_Id)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Logo)
            .WithMany()
            .HasForeignKey(x => x.Logo_File_Id)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Cover)
            .WithMany()
            .HasForeignKey(x => x.Cover_File_Id)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class CommunityMemberConfig : IEntityTypeConfiguration<CommunityMember>
{
    public void Configure(EntityTypeBuilder<CommunityMember> builder)
    {
        builder.ToTable("Comm_Members");
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.Community_Id, x.Employee_Id })
            .IsUnique()
            .HasDatabaseName("UX_Comm_Members");

        // "Which communities am I in" - asked on every feed request, because community
        // membership is part of the viewer's audience key set.
        builder.HasIndex(x => new { x.Employee_Id, x.Membership_Status })
            .HasDatabaseName("IX_Comm_Members_Employee")
            .IncludeProperties(x => new { x.Community_Id, x.Member_Role });

        // The moderator's pending-requests queue.
        builder.HasIndex(x => new { x.Community_Id, x.Membership_Status })
            .HasDatabaseName("IX_Comm_Members_Community_Status");

        builder.HasOne(x => x.Community)
            .WithMany(x => x.Members)
            .HasForeignKey(x => x.Community_Id)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.Employee_Id)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ReviewedBy)
            .WithMany()
            .HasForeignKey(x => x.Reviewed_By_Employee_Id)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class CommunityInvitationConfig : IEntityTypeConfiguration<CommunityInvitation>
{
    public void Configure(EntityTypeBuilder<CommunityInvitation> builder)
    {
        builder.ToTable("Comm_Invitations");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Message).HasMaxLength(ColumnLengths.LongText);

        // One live invitation per person per community. Filtered on Pending so a
        // declined invitation does not block a later, genuine re-invite.
        builder.HasIndex(x => new { x.Community_Id, x.Invited_Employee_Id })
            .IsUnique()
            .HasDatabaseName("UX_Comm_Invitations_Pending")
            .HasFilter("[Status] = 0 AND [Is_Deleted] = 0");

        builder.HasIndex(x => new { x.Invited_Employee_Id, x.Status })
            .HasDatabaseName("IX_Comm_Invitations_Invitee");

        builder.HasOne(x => x.Community)
            .WithMany(x => x.Invitations)
            .HasForeignKey(x => x.Community_Id)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.InvitedEmployee)
            .WithMany()
            .HasForeignKey(x => x.Invited_Employee_Id)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.InvitedBy)
            .WithMany()
            .HasForeignKey(x => x.Invited_By_Employee_Id)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
