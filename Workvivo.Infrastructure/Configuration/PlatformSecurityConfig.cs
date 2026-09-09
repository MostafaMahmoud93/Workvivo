using Workvivo.Infrastructure.Configuration.Conventions;

namespace Workvivo.Infrastructure.Configuration;

public class RefreshTokenConfig : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("Security_RefreshTokens");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Token_Hash).IsRequired().HasMaxLength(ColumnLengths.Sha256Hex);
        builder.Property(x => x.Revoked_Reason).HasMaxLength(ColumnLengths.Description);
        builder.Property(x => x.Created_Ip).HasMaxLength(ColumnLengths.IpAddress);
        builder.Property(x => x.Revoked_Ip).HasMaxLength(ColumnLengths.IpAddress);
        builder.Property(x => x.Created_User_Agent).HasMaxLength(ColumnLengths.UserAgent);

        // Every refresh is a lookup by hash, so this is the hot path of the auth
        // system. Unique because two tokens hashing the same would be a collision that
        // silently merged two sessions.
        builder.HasIndex(x => x.Token_Hash)
            .IsUnique()
            .HasDatabaseName("UX_Security_RefreshTokens_Hash");

        // Replay detection revokes a whole family at once, which needs the family to be
        // reachable in one seek.
        builder.HasIndex(x => x.Family_Id).HasDatabaseName("IX_Security_RefreshTokens_Family");

        // "Sign out everywhere", and the cleanup job that removes expired tokens.
        builder.HasIndex(x => new { x.User_Id, x.Expires_At })
            .HasDatabaseName("IX_Security_RefreshTokens_User");

        builder.HasOne(x => x.User)
            .WithMany(x => x.RefreshTokens)
            .HasForeignKey(x => x.User_Id)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.ReplacedBy)
            .WithMany()
            .HasForeignKey(x => x.Replaced_By_Id)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>
/// Extends the template's existing LinkScreenAction mapping with the named-permission
/// columns. The template's own configuration for this entity is left untouched.
/// </summary>
public class LinkScreenActionPermissionConfig : IEntityTypeConfiguration<LinkScreenAction>
{
    public void Configure(EntityTypeBuilder<LinkScreenAction> builder)
    {
        builder.Property(x => x.Permission_Key).HasMaxLength(ColumnLengths.Code);
        builder.Property(x => x.Module).HasMaxLength(ColumnLengths.Code);
        builder.Property(x => x.Description_Ar).HasMaxLength(ColumnLengths.Description);
        builder.Property(x => x.Description_En).HasMaxLength(ColumnLengths.Description);

        // Filtered unique: the rows the template already seeds have no named
        // permission, and a plain unique index would treat every one of those nulls as
        // a duplicate.
        builder.HasIndex(x => x.Permission_Key)
            .IsUnique()
            .HasDatabaseName("UX_Common_LinkScreenActions_PermissionKey")
            .HasFilter("[Permission_Key] IS NOT NULL");
    }
}

public class NotificationPreferenceConfig : IEntityTypeConfiguration<NotificationPreference>
{
    public void Configure(EntityTypeBuilder<NotificationPreference> builder)
    {
        builder.ToTable("Notif_Preferences");
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.Employee_Id, x.Notification_Type })
            .IsUnique()
            .HasDatabaseName("UX_Notif_Preferences");

        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.Employee_Id)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class AuditLogConfig : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("Audit_Logs");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Username).HasMaxLength(ColumnLengths.Name);
        builder.Property(x => x.Entity_Name).HasMaxLength(ColumnLengths.Code);
        builder.Property(x => x.Entity_Id).HasMaxLength(ColumnLengths.Code);
        builder.Property(x => x.Affected_Columns).HasMaxLength(ColumnLengths.LongText);
        builder.Property(x => x.Ip_Address).HasMaxLength(ColumnLengths.IpAddress);
        builder.Property(x => x.User_Agent).HasMaxLength(ColumnLengths.UserAgent);
        builder.Property(x => x.Correlation_Id).HasMaxLength(ColumnLengths.Code);

        // Old_Values and New_Values are JSON of arbitrary size and are never indexed.

        // "What happened to this record" - the entity-history view.
        builder.HasIndex(x => new { x.Entity_Name, x.Entity_Id, x.Timestamp })
            .HasDatabaseName("IX_Audit_Logs_Entity")
            .IsDescending(false, false, true);

        // "What did this person do" - the user-activity view.
        builder.HasIndex(x => new { x.User_Id, x.Timestamp })
            .HasDatabaseName("IX_Audit_Logs_User")
            .IsDescending(false, true);

        // The default admin listing, and the archival job's range scan.
        builder.HasIndex(x => new { x.Timestamp, x.Action })
            .HasDatabaseName("IX_Audit_Logs_Timeline")
            .IsDescending(true, false);

        // Restrict, and nullable: an audit row has to outlive the account it describes.
        // Cascading would delete the evidence along with the user.
        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.User_Id)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
