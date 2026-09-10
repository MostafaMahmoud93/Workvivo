using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration.UserSecrets;

namespace Workvivo.API.Extensions;

/// <summary>
/// Generates the local development secrets when they are missing, so a fresh clone
/// runs without a setup step.
///
/// The application refuses to start without a signing key, and that refusal is
/// correct - a platform that boots with a guessable key is worse than one that does
/// not boot. But the refusal is only *useful* in an environment where somebody could
/// have supplied a real secret. On a developer's machine it is pure friction: the
/// answer is always "generate a random one", and making a person do that by hand
/// teaches them nothing and costs them an afternoon the first time they hit it.
///
/// So in Development the keys are generated and written to the user-secrets store.
/// Three properties make that safe rather than a hole:
///
/// - It only ever runs when <c>IsDevelopment()</c>. Every other environment still
///   fails loudly, which is what protects production.
/// - The value is cryptographically random and different on every machine. There is
///   no shared default anybody could learn.
/// - It is written to the user-secrets store, which lives outside the repository and
///   cannot be committed. Writing to appsettings.json would put a key in git, which
///   is the thing all of this exists to prevent.
/// </summary>
public static class DevelopmentSecrets
{
    /// <summary>512 bits, base64. Comfortably past the 32-character floor the guards enforce.</summary>
    private const int KeyBytes = 64;

    /// <summary>
    /// Fills in any missing development secret, and reports what it did.
    /// </summary>
    /// <remarks>
    /// Takes the <see cref="IConfigurationManager"/> rather than an
    /// <see cref="IConfiguration"/> because a generated value has to be readable by
    /// the rest of start-up, and only the manager can still take a new source.
    /// </remarks>
    public static void EnsurePresent(
        IConfigurationManager configuration,
        IHostEnvironment environment,
        Serilog.ILogger logger)
    {
        if (!environment.IsDevelopment())
        {
            return;
        }

        var required = new[]
        {
            ("Jwt:SigningKey", "signs access tokens"),
            ("Anonymity:Key", "keeps anonymous poll and survey responses anonymous"),
        };

        var generated = new Dictionary<string, string?>();

        foreach (var (key, purpose) in required)
        {
            if (!string.IsNullOrWhiteSpace(configuration[key]))
            {
                continue;
            }

            generated[key] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(KeyBytes));

            logger.Information(
                "Generated a development {Key} ({Purpose}). It is random, local to this machine, "
                + "and stored in user secrets - never in the repository.",
                key,
                purpose);
        }

        if (generated.Count == 0)
        {
            return;
        }

        // Added to configuration first, so this run works even if persisting fails.
        configuration.AddInMemoryCollection(generated);

        Persist(generated, logger);
    }

    /// <summary>
    /// Writes the generated values into the user-secrets file.
    ///
    /// Persisting matters for more than convenience: a key regenerated on every start
    /// invalidates every token issued by the previous run, so a developer would be
    /// signed out by their own rebuild.
    ///
    /// Best-effort. If the store cannot be located or written, the run still has its
    /// keys from the in-memory source above, and the only cost is that the next
    /// start generates fresh ones.
    /// </summary>
    private static void Persist(Dictionary<string, string?> generated, Serilog.ILogger logger)
    {
        var secretsId = Assembly.GetEntryAssembly()
            ?.GetCustomAttribute<UserSecretsIdAttribute>()
            ?.UserSecretsId;

        if (string.IsNullOrWhiteSpace(secretsId))
        {
            logger.Warning(
                "No UserSecretsId on the entry assembly, so the generated development keys were "
                + "not saved. They will be different after a restart.");
            return;
        }

        try
        {
            var path = PathHelper.GetSecretsPathFromSecretsId(secretsId);

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            // Merged into whatever is already there rather than overwritten - the file
            // may hold connection strings or other secrets this method knows nothing
            // about, and replacing it would silently delete them.
            var document = File.Exists(path)
                ? JsonNode.Parse(File.ReadAllText(path)) as JsonObject ?? []
                : [];

            foreach (var (key, value) in generated)
            {
                document[key] = value;
            }

            File.WriteAllText(path, document.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

            logger.Information("Saved the generated development keys to {Path}", path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            logger.Warning(
                exception,
                "Could not save the generated development keys. They will be different after a restart.");
        }
    }
}
