namespace Workvivo.Infrastructure.Configuration.Conventions;

/// <summary>
/// Column widths, named once instead of scattered as literals across fifty
/// configurations.
///
/// The widths matter beyond tidiness: an unbounded nvarchar(max) cannot take part in
/// an index key, so a column that is ever filtered, sorted or made unique has to be
/// bounded. Everything genuinely free-form - post bodies, JSON audit payloads - stays
/// unbounded and is deliberately never indexed.
/// </summary>
internal static class ColumnLengths
{
    /// <summary>Short machine-readable codes and slugs.</summary>
    public const int Code = 100;

    /// <summary>Names, titles, labels.</summary>
    public const int Name = 200;

    /// <summary>One-paragraph descriptions.</summary>
    public const int Description = 1000;

    /// <summary>Longer free text that still needs a ceiling - recognition messages, notes.</summary>
    public const int LongText = 2000;

    public const int Email = 256;
    public const int Phone = 32;

    /// <summary>Practical upper bound for a URL that browsers and proxies will accept.</summary>
    public const int Url = 2048;

    /// <summary>Hex SHA-256.</summary>
    public const int Sha256Hex = 64;

    /// <summary>Fits an IPv6 address with an embedded IPv4 suffix and a zone index.</summary>
    public const int IpAddress = 45;

    public const int UserAgent = 512;

    /// <summary>
    /// Audience key: a seven-character prefix plus a 36-character GUID. Bounded tightly
    /// because it is the index the whole feed query seeks on.
    /// </summary>
    public const int AudienceKey = 64;

    /// <summary>Materialised department path - deep enough for any real hierarchy.</summary>
    public const int Path = 900;
}
