namespace Workvivo.Infrastructure.Caching;

/// <summary>
/// Every cache key in the system, built here rather than interpolated at call sites.
///
/// Keys have to agree exactly between the code that writes one and the code that
/// invalidates it. A typo in a string literal at one of those two places produces
/// stale data that survives until the TTL, which is close to impossible to reproduce.
/// The shared prefixes also make <c>RemoveByPrefixAsync</c> safe to reason about.
/// </summary>
public static class CacheKeys
{
    public static class Permissions
    {
        public const string Prefix = "perm:";

        public static string ForUser(Guid userId) => $"{Prefix}user:{userId}";
    }

    public static class Audience
    {
        public const string Prefix = "aud:";

        public static string ForEmployee(Guid employeeId) => $"{Prefix}emp:{employeeId}";
    }

    public static class Organization
    {
        public const string Prefix = "org:";

        public const string Tree = Prefix + "tree";

        public static string Department(Guid id) => $"{Prefix}dept:{id}";
    }

    public static class Lookups
    {
        public const string Prefix = "lookup:";

        public static string ByCategory(string category) => $"{Prefix}{category}";
    }

    public static class Notifications
    {
        public const string Prefix = "notif:";

        public static string UnreadCount(Guid userId) => $"{Prefix}unread:{userId}";
    }

    public static class Analytics
    {
        public const string Prefix = "analytics:";
    }
}
