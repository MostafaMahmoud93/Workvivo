using Workvivo.Infrastructure.Configuration.Conventions;

namespace Workvivo.Infrastructure.Configuration;

public class EventConfig : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("Evt_Events");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title_Ar).IsRequired().HasMaxLength(ColumnLengths.Name);
        builder.Property(x => x.Title_En).HasMaxLength(ColumnLengths.Name);
        builder.Property(x => x.Description_Ar).HasMaxLength(ColumnLengths.Description);
        builder.Property(x => x.Description_En).HasMaxLength(ColumnLengths.Description);
        builder.Property(x => x.Address_Ar).HasMaxLength(ColumnLengths.Description);
        builder.Property(x => x.Address_En).HasMaxLength(ColumnLengths.Description);
        builder.Property(x => x.Meeting_Url).HasMaxLength(ColumnLengths.Url);
        builder.Property(x => x.TimeZone_Id).HasMaxLength(ColumnLengths.Code);

        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.Ignore(x => x.DomainEvents);

        // The calendar: published events in a date range.
        builder.HasIndex(x => new { x.Status, x.Is_Deleted, x.Start_At })
            .HasDatabaseName("IX_Evt_Events_Calendar")
            .IncludeProperties(x => new { x.Title_Ar, x.Title_En, x.End_At, x.Event_Type, x.Location_Id });

        // The reminder job: published events starting soon that have not been reminded
        // about yet. Filtered so it never scans past events.
        builder.HasIndex(x => x.Start_At)
            .HasDatabaseName("IX_Evt_Events_PendingReminders")
            .HasFilter("[Reminder_Sent_At] IS NULL AND [Status] = 1 AND [Is_Deleted] = 0");

        builder.HasIndex(x => x.Community_Id).HasDatabaseName("IX_Evt_Events_Community");

        builder.HasOne(x => x.Location)
            .WithMany()
            .HasForeignKey(x => x.Location_Id)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Organizer)
            .WithMany()
            .HasForeignKey(x => x.Organizer_Employee_Id)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Community)
            .WithMany()
            .HasForeignKey(x => x.Community_Id)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Banner)
            .WithMany()
            .HasForeignKey(x => x.Banner_File_Id)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Evt_Events_EndAfterStart",
            "[End_At] >= [Start_At]"));

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Evt_Events_CapacityPositive",
            "[Capacity] IS NULL OR [Capacity] > 0"));
    }
}

public class EventAttendeeConfig : IEntityTypeConfiguration<EventAttendee>
{
    public void Configure(EntityTypeBuilder<EventAttendee> builder)
    {
        builder.ToTable("Evt_Attendees");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Note).HasMaxLength(ColumnLengths.Description);

        builder.HasIndex(x => new { x.Event_Id, x.Employee_Id })
            .IsUnique()
            .HasDatabaseName("UX_Evt_Attendees");

        // "My upcoming events", and the attendee list grouped by response.
        builder.HasIndex(x => new { x.Employee_Id, x.Response })
            .HasDatabaseName("IX_Evt_Attendees_Employee");

        builder.HasOne(x => x.Event)
            .WithMany(x => x.Attendees)
            .HasForeignKey(x => x.Event_Id)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.Employee_Id)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class EventAudienceConfig : IEntityTypeConfiguration<EventAudience>
{
    public void Configure(EntityTypeBuilder<EventAudience> builder)
    {
        AudienceConfiguration.Apply(builder, "Evt_Audiences", nameof(EventAudience.Event_Id));

        builder.HasOne(x => x.Event)
            .WithMany(x => x.Audiences)
            .HasForeignKey(x => x.Event_Id)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
