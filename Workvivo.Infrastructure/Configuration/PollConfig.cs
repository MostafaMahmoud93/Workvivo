using Workvivo.Infrastructure.Configuration.Conventions;

namespace Workvivo.Infrastructure.Configuration;

public class PollConfig : IEntityTypeConfiguration<Poll>
{
    public void Configure(EntityTypeBuilder<Poll> builder)
    {
        builder.ToTable("Poll_Polls");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Question_Ar).IsRequired().HasMaxLength(ColumnLengths.Description);
        builder.Property(x => x.Question_En).HasMaxLength(ColumnLengths.Description);
        builder.Ignore(x => x.DomainEvents);

        builder.HasIndex(x => x.Post_Id).HasDatabaseName("IX_Poll_Polls_Post");

        // The job that closes expired polls.
        builder.HasIndex(x => new { x.Status, x.Expiry_Date })
            .HasDatabaseName("IX_Poll_Polls_Expiry")
            .HasFilter("[Expiry_Date] IS NOT NULL AND [Is_Deleted] = 0");

        builder.HasOne(x => x.Post)
            .WithMany()
            .HasForeignKey(x => x.Post_Id)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class PollOptionConfig : IEntityTypeConfiguration<PollOption>
{
    public void Configure(EntityTypeBuilder<PollOption> builder)
    {
        builder.ToTable("Poll_Options");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Text_Ar).IsRequired().HasMaxLength(ColumnLengths.Name);
        builder.Property(x => x.Text_En).HasMaxLength(ColumnLengths.Name);

        builder.HasIndex(x => x.Poll_Id).HasDatabaseName("IX_Poll_Options_Poll");

        builder.HasOne(x => x.Poll)
            .WithMany(x => x.Options)
            .HasForeignKey(x => x.Poll_Id)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class PollVoteConfig : IEntityTypeConfiguration<PollVote>
{
    public void Configure(EntityTypeBuilder<PollVote> builder)
    {
        builder.ToTable("Poll_Votes");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Voter_Hash).HasMaxLength(ColumnLengths.Sha256Hex);

        // One vote per option per identified voter. Filtered, because on an anonymous
        // poll Employee_Id is null and SQL Server would otherwise treat every null as
        // a distinct value and enforce nothing at all.
        builder.HasIndex(x => new { x.Poll_Id, x.Option_Id, x.Employee_Id })
            .IsUnique()
            .HasDatabaseName("UX_Poll_Votes_Identified")
            .HasFilter("[Employee_Id] IS NOT NULL");

        // The same rule for anonymous polls, keyed on the hash instead of the identity.
        builder.HasIndex(x => new { x.Poll_Id, x.Option_Id, x.Voter_Hash })
            .IsUnique()
            .HasDatabaseName("UX_Poll_Votes_Anonymous")
            .HasFilter("[Voter_Hash] IS NOT NULL");

        builder.HasOne(x => x.Poll)
            .WithMany(x => x.Votes)
            .HasForeignKey(x => x.Poll_Id)
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict: Poll -> Option -> Vote and Poll -> Vote are two cascade paths to
        // the same table, which SQL Server rejects outright.
        builder.HasOne(x => x.Option)
            .WithMany(x => x.Votes)
            .HasForeignKey(x => x.Option_Id)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.Employee_Id)
            .OnDelete(DeleteBehavior.Restrict);

        // Anonymity has to be structural: exactly one of the two identifiers is
        // present, so an "anonymous" vote cannot quietly carry an employee id.
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Poll_Votes_OneVoterIdentity",
            "([Employee_Id] IS NOT NULL AND [Voter_Hash] IS NULL) OR ([Employee_Id] IS NULL AND [Voter_Hash] IS NOT NULL)"));
    }
}

public class PollAudienceConfig : IEntityTypeConfiguration<PollAudience>
{
    public void Configure(EntityTypeBuilder<PollAudience> builder)
    {
        AudienceConfiguration.Apply(builder, "Poll_Audiences", nameof(PollAudience.Poll_Id));

        builder.HasOne(x => x.Poll)
            .WithMany(x => x.Audiences)
            .HasForeignKey(x => x.Poll_Id)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
