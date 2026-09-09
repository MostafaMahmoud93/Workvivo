using Hangfire;
using Hangfire.SqlServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Infrastructure.Caching;
using Workvivo.Infrastructure.Common;
using Workvivo.Infrastructure.Jobs;
using Workvivo.Infrastructure.Storage;

namespace Workvivo.Infrastructure;

/// <summary>
/// Composition root for everything that talks to the outside world.
///
/// Which implementation of an abstraction gets registered is decided here from
/// configuration, so moving from local disk to Azure Blob, or from in-process caching
/// to Redis, is an environment variable rather than a code change.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureLayer(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHttpContextAccessor();

        services.AddOptions<StorageOptions>()
            .Bind(configuration.GetSection(StorageOptions.SectionName))
            .ValidateOnStart();

        services.AddOptions<FileValidationOptions>()
            .Bind(configuration.GetSection(FileValidationOptions.SectionName))
            .ValidateOnStart();

        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddSingleton<IContentSanitizer, HtmlSanitizerAdapter>();

        AddCaching(services, configuration);
        AddStorage(services, configuration);
        AddBackgroundJobs(services, configuration);

        return services;
    }

    private static void AddCaching(IServiceCollection services, IConfiguration configuration)
    {
        var redisConnection = configuration.GetConnectionString("Redis")
            ?? configuration["Redis:ConnectionString"];

        if (!string.IsNullOrWhiteSpace(redisConnection))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnection;
                options.InstanceName = configuration["Redis:InstanceName"] ?? "workvivo:";
            });
        }
        else
        {
            // Same IDistributedCache contract, single-process storage. Keeps the
            // programming model identical whether or not Redis is present.
            services.AddDistributedMemoryCache();
        }

        services.AddSingleton<ICacheService, DistributedCacheService>();
    }

    private static void AddStorage(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IFileScanner, NullFileScanner>();

        var provider = configuration[$"{StorageOptions.SectionName}:Provider"] ?? "Local";

        // Azure Blob and S3 providers are added in the file-handling phase; until then
        // an explicit failure at startup beats silently writing production uploads to
        // a container's local disk, where the next deployment deletes them.
        services.AddSingleton<IFileStorageService>(sp => provider.ToLowerInvariant() switch
        {
            "local" => ActivatorUtilities.CreateInstance<LocalFileStorageService>(sp),
            _ => throw new InvalidOperationException(
                $"Storage provider '{provider}' is configured but not implemented yet. Use 'Local'."),
        });
    }

    private static void AddBackgroundJobs(IServiceCollection services, IConfiguration configuration)
    {
        var enabled = configuration.GetValue("BackgroundJobs:Enabled", false);

        if (!enabled)
        {
            services.AddSingleton<IBackgroundJobScheduler, InlineBackgroundJobScheduler>();
            return;
        }

        var connectionString = configuration.GetConnectionString("WorkvivoConnStr");

        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(connectionString, new SqlServerStorageOptions
            {
                // Sliding invisibility rather than the legacy transaction-scoped fetch:
                // it does not hold a transaction open for the length of the job.
                SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                QueuePollInterval = TimeSpan.Zero,
                UseRecommendedIsolationLevel = true,
                DisableGlobalLocks = true,
                SchemaName = "hangfire",
            }));

        services.AddHangfireServer(options =>
        {
            options.WorkerCount = Math.Min(Environment.ProcessorCount * 2, 20);
            options.Queues = ["critical", "default", "notifications", "email", "analytics"];
        });

        services.AddSingleton<IBackgroundJobScheduler, HangfireBackgroundJobScheduler>();
    }
}
