using MediatR;
using Workvivo.Application.Bases;
using Workvivo.Application.Features.Search.Dtos;
using Workvivo.Application.Features.Search.Queries.Search;

namespace Workvivo.API.Controllers.Platform;

/// <summary>
/// One search across the product.
///
/// No permission attribute, because the answer is not the same for two people: every
/// branch of the query is filtered by the caller's audience key set, so two employees
/// searching the same word see different results. A permission here would be a
/// coarser second opinion on a question the query already answers precisely.
/// </summary>
public class SearchController : ApiControllersBase
{
    private readonly ISender _sender;

    public SearchController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [Route(RouteClass.SearchRoutes.Search)]
    public async Task<IActionResult> Search(
        [FromQuery] SearchQuery query,
        CancellationToken cancellationToken) =>
        Ok(ApiResponse<SearchResultsDto>.Ok(await _sender.Send(query, cancellationToken)));
}
