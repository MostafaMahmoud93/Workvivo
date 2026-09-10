using Hangfire;
using Hangfire.Dashboard;
using Workvivo.Application.Features.Notifications.Jobs;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Abstractions.Interfaces;

namespace Workvivo.API.Extensions;

/// <summary>
/// Registers the recurring work and, in development only, the Hangfire dashboard.
/// </summary>
public static class ConfigureBackgroundJobs
{
    /// <summary>
    /// Declares the recurring jobs.
    ///
    /// Registered from code at startup rather than configured in the dashboard, so a
    /// fresh environment has them without anybody remembering to add them - and so the
    /// schedule is reviewable in a diff. <c>AddOrUpdate</c> semantics mean redeploying
    /// with a changed cron updates the existing job rather than duplicating it.
    ///
    /// Times are UTC, which is what the server runs on. A digest that should land at
    /// 08:00 local is a per-timezone schedule and is not attempted here; the daily one
    /// goes out at 07:00 UTC, which is early morning across the Gulf offices this
    /// product is built for.
    /// </summary>
    public static void RegisterRecurringJobs(this IServiceProvider services)
    {
        var scheduler = services.GetRequiredService<IBackgroundJobScheduler>();

        // Every minute. Cheap because "nothing due" is an index seek returning no rows,
        // and a scheduled post is not worth more precision than this.
        scheduler.AddOrUpdateRecurring<IScheduledPostPublishingJob>(
            "posts:publish-scheduled",
            job => job.PublishDueAsync(),
            "* * * * *");

        // Nightly, off-peak. Corrects counters that drifted because something wrote to
        // the tables without going through the application.
        scheduler.AddOrUpdateRecurring<ICounterReconciliationJob>(
            "feed:reconcile-counters",
            job => job.ReconcileAsync(),
            "30 2 * * *");

        scheduler.AddOrUpdateRecurring<INotificationDigestJob>(
            "notifications:digest-hourly",
            job => job.SendAsync((int)NotificationDigestFrequency.Hourly),
            "5 * * * *");

        scheduler.AddOrUpdateRecurring<INotificationDigestJob>(
            "notifications:digest-daily",
            job => job.SendAsync((int)NotificationDigestFrequency.Daily),
            "0 7 * * *");

        // Monday morning, so a weekly digest arrives when people are back rather than
        // over the weekend.
        scheduler.AddOrUpdateRecurring<INotificationDigestJob>(
            "notifications:digest-weekly",
            job => job.SendAsync((int)NotificationDigestFrequency.Weekly),
            "0 7 * * MON");
    }

    /// <summary>
    /// Mounts the Hangfire dashboard, in development only.
    ///
    /// It is not exposed in other environments on purpose. The dashboard lets anyone
    /// who reaches it requeue and delete jobs, and its own authorisation filter runs
    /// against a browser navigation - which carries no Authorization header, because
    /// this API authenticates with a bearer token held in memory by the SPA. There is
    /// no way to authenticate that navigation correctly today, and shipping the
    /// dashboard behind a filter that cannot see the caller would be worse than not
    /// shipping it: it would look protected.
    ///
    /// Operating it in production means either a cookie scheme scoped to the dashboard
    /// path or reaching it through an authenticated reverse proxy. Both are deliberate
    /// decisions for the deployment phase, not defaults.
    /// </summary>
    public static void MapWorkvivoJobDashboard(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            return;
        }

        if (!app.Configuration.GetValue("BackgroundJobs:Enabled", false))
        {
            // No Hangfire storage is configured when jobs are disabled, and the
            // dashboard throws rather than showing an empty page.
            return;
        }

        app.MapHangfireDashboard("/jobs", new DashboardOptions
        {
            Authorization = [new LocalRequestsOnlyFilter()],
            DisplayStorageConnectionString = false,
        });
    }
}

/// <summary>
/// Lets the dashboard through only for a request that came from the machine it runs
/// on.
///
/// Weak by design and only ever combined with "development only" above. It exists so
/// that a developer's browser works and a container with a published port does not
/// hand the job queue to the network.
/// </summary>
public sealed class LocalRequestsOnlyFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var connection = context.GetHttpContext().Connection;

        var remote = connection.RemoteIpAddress;
        var local = connection.LocalIpAddress;

        if (remote is null)
        {
            return false;
        }

        return System.Net.IPAddress.IsLoopback(remote)
            || (local is not null && remote.Equals(local));
    }
}
