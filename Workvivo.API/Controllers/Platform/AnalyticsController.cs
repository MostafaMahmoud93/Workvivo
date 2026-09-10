using MediatR;
using Workvivo.Application.Bases;
using Workvivo.Application.Features.Analytics.Dtos;
using Workvivo.Application.Features.Analytics.Queries.GetDashboard;
using Workvivo.Infrastructure.Seeding;

namespace Workvivo.API.Controllers.Platform;

/// <summary>
/// Engagement analytics.
///
/// Aggregate only. There is no per-employee activity report and there is not going
/// to be one: a tool that tells a manager who has not posted this week changes what
/// an internal network is for.
/// </summary>
public class AnalyticsController : ApiControllersBase
{
    private readonly ISender _sender;

    public AnalyticsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [Route(RouteClass.AnalyticsRoutes.Dashboard)]
    [HasPermission(Permissions.Analytics.View)]
    public async Task<IActionResult> Dashboard(
        [FromQuery] GetAnalyticsDashboardQuery query,
        CancellationToken cancellationToken) =>
        Ok(ApiResponse<AnalyticsDashboardDto>.Ok(await _sender.Send(query, cancellationToken)));
}
