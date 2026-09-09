namespace Workvivo.API.Options;

/// <summary>
/// Allowed browser origins, from configuration rather than hard-coded.
///
/// The template pinned localhost:4800 in Program.cs, which means a deployed
/// environment either edits source or turns CORS off. Neither is acceptable, and
/// AllowAnyOrigin cannot be combined with credentials anyway.
/// </summary>
public sealed class CorsOptions
{
    public const string SectionName = "Cors";

    public const string PolicyName = "WorkvivoSpa";

    public string[] AllowedOrigins { get; set; } = [];
}
