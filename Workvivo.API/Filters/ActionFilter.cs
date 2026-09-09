namespace Workvivo.API.Filters;
public class ActionFilter : IActionFilter
{
    private readonly IAuthService _authService;
    public ActionFilter(IAuthService authService)
    {
        _authService = authService;
    }


    public void OnActionExecuted(ActionExecutedContext context)
    {

    }


    public void OnActionExecuting(ActionExecutingContext context)
    {
        bool hasAllowAnonymous = context.ActionDescriptor.EndpointMetadata.Any(em => em.GetType() == typeof(AllowAnonymousAttribute));
        if (!hasAllowAnonymous)
        {
            if (AllowFiltered.Controllers.Contains(context.ActionDescriptor.RouteValues["controller"]) || AllowFiltered.Actions.Contains(context.ActionDescriptor.RouteValues["action"]))
                return;

            PathString path = context.HttpContext.Request.Path;

            string[] splittedUrl = path.ToString().TrimStart('/').Split('/');
            if (splittedUrl.Length < 3)
            {
                ServiceResponse<int> response = new ServiceResponse<int> { Success = true, Data = 403, Message = $"Invalid URL format: {path}" };
                context.Result = new UnauthorizedObjectResult(response);
            }
            string baseRoute = $"/{string.Join('/', splittedUrl.Take(3))}";
            ServiceResponse<bool> res = _authService.IfUserHasActions(baseRoute).Result;
            if (res.Data)
            {
                ServiceResponse<int> response = new ServiceResponse<int> { Success = true, Data = 403, Message = res.Message };
                context.Result = new UnauthorizedObjectResult(response);
            }
        }

    }


}
