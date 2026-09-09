namespace Workvivo.Infrastructure.Storage;

/// <summary>Bound from the "Storage" configuration section.</summary>
public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>"Local", "AzureBlob" or "AmazonS3". Decides which provider is registered.</summary>
    public string Provider { get; set; } = "Local";

    /// <summary>
    /// Root directory for the local provider. Must sit outside the web root - anything
    /// reachable by static-file middleware is an unauthenticated download endpoint for
    /// every file in the system.
    /// </summary>
    public string LocalRootPath { get; set; } = string.Empty;

    /// <summary>Azure Blob or S3 connection string. Comes from the environment, never from appsettings.</summary>
    public string? ConnectionString { get; set; }

    /// <summary>Container or bucket name for the cloud providers.</summary>
    public string? ContainerName { get; set; }

    /// <summary>How long a presigned download link stays valid.</summary>
    public TimeSpan PresignedUrlLifetime { get; set; } = TimeSpan.FromMinutes(10);
}
