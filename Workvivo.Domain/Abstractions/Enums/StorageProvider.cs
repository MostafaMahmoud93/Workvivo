namespace Workvivo.Domain.Abstractions.Enums;

/// <summary>Which backing store holds a file's bytes. Persisted on every file row.</summary>
public enum StorageProvider
{
    Local = 0,
    AzureBlob = 1,
    AmazonS3 = 2,
}
