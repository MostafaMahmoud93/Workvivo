using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Workvivo.Application.Behaviors;
using Workvivo.Application.Bases;
using Workvivo.Application.Common.Events;
using Workvivo.Application.Common.Security;
using Workvivo.Application.Features.Notifications.Common;
using Workvivo.Application.Features.Notifications.Jobs;

namespace Workvivo.Application;

/// <summary>
/// Composition root for the use-case layer: MediatR, its pipeline, the validators and
/// the mapping profiles.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplicationLayer(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(assembly);

            // Registration order is execution order, outermost first, and it matters:
            //
            //   logging      wraps everything, so a rejected request is still recorded
            //   performance  measures the real cost, validation included
            //   validation   runs before any work, so a bad request never opens a
            //                transaction or warms a cache entry
            //   caching      sits inside validation but outside the handler
            //   events       immediately outside the transaction, so a domain event is
            //                published only after the write it describes has committed
            //   transaction  innermost, around the handler alone, held for as short a
            //                time as possible
            config.AddOpenBehavior(typeof(RequestLoggingBehavior<,>));
            config.AddOpenBehavior(typeof(PerformanceBehavior<,>));
            config.AddOpenBehavior(typeof(ValidationBehavior<,>));
            config.AddOpenBehavior(typeof(CachingBehavior<,>));
            config.AddOpenBehavior(typeof(DomainEventDispatchBehavior<,>));
            config.AddOpenBehavior(typeof(TransactionBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        // The one place a notification is created. Scoped, because it writes through
        // the request's unit of work.
        services.AddScoped<CurrentEmployee>();
        services.AddScoped<INotificationDispatcher, NotificationDispatcher>();
        services.AddScoped<IPublisherAdapter, PublisherAdapter>();
        services.AddSingleton<NotificationEmailRenderer>();

        // Background jobs, registered by their interfaces because that is what the
        // scheduler serialises into a queued job. Scoped: each runs in its own scope
        // with its own unit of work, long after the request that queued it is gone.
        services.AddScoped<INotificationEmailJob, NotificationEmailJob>();
        services.AddScoped<INotificationDigestJob, NotificationDigestJob>();
        services.AddScoped<IScheduledPostPublishingJob, ScheduledPostPublishingJob>();
        services.AddScoped<ICounterReconciliationJob, CounterReconciliationJob>();
        services.AddScoped<IAnnouncementFanOutJob, AnnouncementFanOutJob>();
        services.AddScoped<Features.Recognition.Jobs.ILeaderboardSnapshotJob,
            Features.Recognition.Jobs.LeaderboardSnapshotJob>();
        services.AddScoped<Features.Events.Jobs.IEventReminderJob, Features.Events.Jobs.EventReminderJob>();

        // Registers every Profile in this assembly (MapperProfile/* and the per-feature
        // Mappings folders). Every service here injects IMapper, so without this the
        // container cannot construct a single one of them.
        services.AddAutoMapper(_ => { }, typeof(MappingProfileBase).Assembly);

        return services;
    }
}
