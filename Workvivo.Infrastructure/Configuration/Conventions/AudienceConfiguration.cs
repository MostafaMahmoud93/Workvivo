using Workvivo.Domain.Entities.BaseEntities;

namespace Workvivo.Infrastructure.Configuration.Conventions;

/// <summary>
/// The mapping every audience table shares.
///
/// Five tables have identical audience columns. Repeating the widths, the index and
/// the uniqueness rule five times is how one of them ends up subtly different - and a
/// missing index here does not fail, it just makes the feed slow at exactly the scale
/// where that matters.
/// </summary>
internal static class AudienceConfiguration
{
    /// <summary>
    /// Applies the shared audience mapping.
    /// </summary>
    /// <param name="ownerKeyName">
    /// The owning foreign key column, for example <c>Post_Id</c>. Paired with the
    /// audience key in a unique index so the same owner cannot carry the same target
    /// twice - a duplicate would multiply rows out of the feed's EXISTS join.
    /// </param>
    public static void Apply<TEntity>(
        EntityTypeBuilder<TEntity> builder,
        string tableName,
        string ownerKeyName)
        where TEntity : AudienceEntity<Guid>
    {
        builder.ToTable(tableName);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Audience_Type).IsRequired();

        builder.Property(x => x.Audience_Key)
            .IsRequired()
            .HasMaxLength(ColumnLengths.AudienceKey);

        builder.HasIndex(ownerKeyName, nameof(AudienceEntity<Guid>.Audience_Key))
            .IsUnique()
            .HasDatabaseName($"UX_{tableName}_Owner_Key");

        // The index the read path lives on: given the viewer's handful of audience
        // keys, find the owning rows. Covering, so resolving a key never has to touch
        // the table itself.
        builder.HasIndex(nameof(AudienceEntity<Guid>.Audience_Key))
            .HasDatabaseName($"IX_{tableName}_Key")
            .IncludeProperties(ownerKeyName);
    }
}
