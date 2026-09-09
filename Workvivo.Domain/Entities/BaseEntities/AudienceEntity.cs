using Workvivo.Domain.Abstractions.Enums;

namespace Workvivo.Domain.Entities.BaseEntities;

/// <summary>
/// Shared shape for every audience-targeting table - post, event, survey, poll and
/// document audiences all derive from this.
///
/// The tables stay separate so each keeps a real foreign key to its owner rather
/// than a polymorphic id with no referential integrity. Only the *shape* is shared,
/// and with it one invariant that matters a great deal:
///
///   <see cref="Audience_Key"/> is what the feed and document queries filter on, and
///   it is derived from <see cref="Audience_Type"/> and <see cref="Target_Id"/>. If
///   the three ever disagree - someone sets the type and forgets the key - the row
///   silently matches the wrong viewers, and nothing fails loudly. So the setters are
///   private and all three are written together, through <see cref="Retarget"/>.
///
/// Not itself mapped: EF sees only the derived types, each with its own table.
/// </summary>
public abstract class AudienceEntity<TKey> : BaseCommonEntity<TKey>
{
    /// <summary>Parameterless constructor for EF materialisation and lazy-loading proxies.</summary>
    protected AudienceEntity()
    {
    }

    protected AudienceEntity(AudienceType audienceType, Guid? targetId)
    {
        Retarget(audienceType, targetId);
    }

    public AudienceType Audience_Type { get; private set; }

    /// <summary>
    /// The department, team, location, job title, role, community or employee being
    /// targeted. Null - and only null - for <see cref="AudienceType.AllEmployees"/>.
    /// </summary>
    public Guid? Target_Id { get; private set; }

    /// <summary>
    /// Denormalised lookup key, for example <c>DEPT:{guid}</c> or <c>ALL</c>.
    ///
    /// Exists so the feed reduces to a single indexed <c>IN (@keys)</c> against the
    /// viewer's small key set, instead of a union of one predicate per audience
    /// dimension.
    /// </summary>
    public string Audience_Key { get; private set; } = string.Empty;

    /// <summary>Points this row at a new target, keeping all three columns consistent.</summary>
    public void Retarget(AudienceType audienceType, Guid? targetId)
    {
        if (audienceType == AudienceType.AllEmployees)
        {
            if (targetId is not null)
            {
                throw new ArgumentException(
                    "AllEmployees targets the whole organisation and cannot carry a target id.",
                    nameof(targetId));
            }
        }
        else if (targetId is null || targetId == Guid.Empty)
        {
            throw new ArgumentException(
                $"Audience type {audienceType} requires a target id.",
                nameof(targetId));
        }

        Audience_Type = audienceType;
        Target_Id = targetId;
        Audience_Key = AudienceKey.For(audienceType, targetId);
    }
}

/// <summary>
/// Builds and parses the audience key.
///
/// The prefixes are persisted in every audience row and cached against every
/// employee, so they are a storage format: changing one invalidates both and needs a
/// migration, not an edit.
/// </summary>
public static class AudienceKey
{
    public const string All = "ALL";

    public static string For(AudienceType audienceType, Guid? targetId) => audienceType switch
    {
        AudienceType.AllEmployees => All,
        AudienceType.Department => $"DEPT:{targetId:D}",
        AudienceType.Team => $"TEAM:{targetId:D}",
        AudienceType.Location => $"LOC:{targetId:D}",
        AudienceType.JobTitle => $"JOB:{targetId:D}",
        AudienceType.Role => $"ROLE:{targetId:D}",
        AudienceType.Community => $"COMM:{targetId:D}",
        AudienceType.Employee => $"EMP:{targetId:D}",
        _ => throw new ArgumentOutOfRangeException(nameof(audienceType), audienceType, "Unknown audience type."),
    };
}
