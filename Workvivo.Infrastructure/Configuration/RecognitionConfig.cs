using Workvivo.Infrastructure.Configuration.Conventions;

namespace Workvivo.Infrastructure.Configuration;

public class RecognitionTypeConfig : IEntityTypeConfiguration<RecognitionType>
{
    public void Configure(EntityTypeBuilder<RecognitionType> builder)
    {
        builder.ToTable("Rec_RecognitionTypes");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code).IsRequired().HasMaxLength(ColumnLengths.Code);
        builder.Property(x => x.Name_Ar).IsRequired().HasMaxLength(ColumnLengths.Name);
        builder.Property(x => x.Name_En).HasMaxLength(ColumnLengths.Name);
        builder.Property(x => x.Description_Ar).HasMaxLength(ColumnLengths.Description);
        builder.Property(x => x.Description_En).HasMaxLength(ColumnLengths.Description);
        builder.Property(x => x.Badge_Icon).HasMaxLength(ColumnLengths.Code);
        builder.Property(x => x.Badge_Color).HasMaxLength(16);

        builder.HasIndex(x => x.Code).IsUnique().HasDatabaseName("UX_Rec_RecognitionTypes_Code");
    }
}

public class RecognitionConfig : IEntityTypeConfiguration<Recognition>
{
    public void Configure(EntityTypeBuilder<Recognition> builder)
    {
        builder.ToTable("Rec_Recognitions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Message).IsRequired().HasMaxLength(ColumnLengths.LongText);
        builder.Ignore(x => x.DomainEvents);

        // A profile's "recognition received", newest first, and the source the
        // leaderboard job aggregates over.
        builder.HasIndex(x => new { x.Recipient_Employee_Id, x.Recognised_On })
            .HasDatabaseName("IX_Rec_Recognitions_Recipient")
            .IsDescending(false, true);

        builder.HasIndex(x => new { x.Sender_Employee_Id, x.Recognised_On })
            .HasDatabaseName("IX_Rec_Recognitions_Sender")
            .IsDescending(false, true);

        builder.HasOne(x => x.Sender)
            .WithMany()
            .HasForeignKey(x => x.Sender_Employee_Id)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Recipient)
            .WithMany()
            .HasForeignKey(x => x.Recipient_Employee_Id)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.RecognitionType)
            .WithMany(x => x.Recognitions)
            .HasForeignKey(x => x.Recognition_Type_Id)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Post)
            .WithMany()
            .HasForeignKey(x => x.Post_Id)
            .OnDelete(DeleteBehavior.Restrict);

        // The domain refuses self-recognition for application code; this refuses it for
        // everything else. Points feed a leaderboard, so the rule is worth stating in
        // both places.
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Rec_Recognitions_NotSelf",
            "[Sender_Employee_Id] <> [Recipient_Employee_Id]"));

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Rec_Recognitions_PointsNotNegative",
            "[Points] >= 0"));
    }
}

public class RecognitionLeaderboardSnapshotConfig : IEntityTypeConfiguration<RecognitionLeaderboardSnapshot>
{
    public void Configure(EntityTypeBuilder<RecognitionLeaderboardSnapshot> builder)
    {
        builder.ToTable("Rec_LeaderboardSnapshots");
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.Employee_Id, x.Period, x.Period_Start, x.Department_Id })
            .IsUnique()
            .HasDatabaseName("UX_Rec_LeaderboardSnapshots");

        // Reading the leaderboard itself: one period, in rank order.
        builder.HasIndex(x => new { x.Period, x.Period_Start, x.Department_Id, x.Rank })
            .HasDatabaseName("IX_Rec_LeaderboardSnapshots_Ranking")
            .IncludeProperties(x => new { x.Employee_Id, x.Points, x.Recognition_Count });

        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.Employee_Id)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Department)
            .WithMany()
            .HasForeignKey(x => x.Department_Id)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
