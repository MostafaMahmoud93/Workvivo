namespace Workvivo.Application.Features.Admin.Dtos;

/// <summary>One role, and how many people hold it.</summary>
public sealed class RoleDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int MemberCount { get; init; }
    public int PermissionCount { get; init; }

    /// <summary>
    /// True for the roles the platform seeds and depends on.
    ///
    /// Surfaced so the client can stop somebody deleting the role their own access
    /// comes from - a mistake that is quick to make and slow to undo.
    /// </summary>
    public bool IsSystem { get; init; }
}

/// <summary>A permission, and whether the role being edited holds it.</summary>
public sealed class PermissionGrantDto
{
    public Guid Id { get; init; }
    public string Key { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Group { get; init; } = string.Empty;
    public bool Granted { get; init; }
}

public sealed class AuditEntryDto
{
    public long Id { get; init; }
    public DateTime Timestamp { get; init; }
    public string? Username { get; init; }
    public string Action { get; init; } = string.Empty;
    public string? EntityName { get; init; }
    public string? EntityId { get; init; }
    public string? AffectedColumns { get; init; }
    public string? IpAddress { get; init; }
    public string? CorrelationId { get; init; }
}
