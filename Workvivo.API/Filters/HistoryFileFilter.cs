namespace Workvivo.API.Filters;
public class HistoryFileFilter : IActionFilter
{
    private readonly IAccessLogService _accessLogService;
    public HistoryFileFilter(IAccessLogService accessLogService)
    {
        _accessLogService = accessLogService;
    }
    public void OnActionExecuted(ActionExecutedContext context)
    {
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        string? requestNo = string.Empty;
        string? notes = string.Empty;
        string? domain = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().DomainName;
        string? ip = string.Empty;
        #region requestNo and notes
        //if (new string[] { "GetCertificates", "GetCertificateTemplatesForSmartApproval" }.Any(action => action == context.ActionDescriptor.RouteValues["action"]))
        //{
        //    CertificatesIssuedFilterModel request = context.ActionArguments.FirstOrDefault().Value as CertificatesIssuedFilterModel;
        //    requestNo = request.CertificateTemplateID.ToString();
        //}
        //if (context.ActionDescriptor.RouteValues["action"] == "CreateCertificate")
        //{
        //    CreateCertificateModel request = context.ActionArguments.FirstOrDefault().Value as CreateCertificateModel;
        //    requestNo = request.CertificateTemplateID.ToString();
        //    notes = string.Concat("شهادة بإسم : ", request.CertificateVariables.FirstOrDefault(a => a.VariableDescription == "الاسم")?.Value);
        //}
        //if (context.ActionDescriptor.RouteValues["action"] == "ApproveCertificate")
        //{
        //    List<int> request = context.ActionArguments.FirstOrDefault().Value as List<int>;
        //    notes = string.Join(",", request);
        //}
        //if (new string[] { "GetApprovedCertificate", "GetCertificateTemplate", "GetCertificateTemplateById", "ApproveCertificateTemplate", "DeleteCertificateTemplate" }.Any(action => action == context.ActionDescriptor.RouteValues["action"]))
        //{
        //    requestNo = context.ActionArguments.FirstOrDefault().Value.ToString();
        //}
        //if (new string[] { "SaveCertificateTemplate", "SendCertificateTemplate" }.Any(action => action == context.ActionDescriptor.RouteValues["action"]))
        //{
        //    SaveCertificateTemplateModel request = context.ActionArguments.FirstOrDefault().Value as SaveCertificateTemplateModel;
        //    requestNo = request.Id.ToString();
        //    notes = string.Concat(request.NameAr, " - ", request.NameEn);
        //}
        //if (context.ActionDescriptor.RouteValues["action"] == "SaveCertificateTemplateAttachment")
        //{
        //    CertificateTemplateAttachmentModel request = context.ActionArguments.FirstOrDefault().Value as CertificateTemplateAttachmentModel;
        //    notes = request.Id.ToString();
        //}
        #endregion

        var path = context.HttpContext.Request.Path;
        bool hasAllowAnonymous = context.ActionDescriptor.EndpointMetadata.Any(em => em.GetType() == typeof(AllowAnonymousAttribute));
        if (!hasAllowAnonymous)
        {
            if (AllowFiltered.Controllers.Contains(context.ActionDescriptor.RouteValues["controller"]) || AllowFiltered.Actions.Contains(context.ActionDescriptor.RouteValues["action"]))
                return;

            if (path != null)
                _accessLogService.AddHistoryFile(path, requestNo, notes, domain, ip).Wait();
        }
    }
}
