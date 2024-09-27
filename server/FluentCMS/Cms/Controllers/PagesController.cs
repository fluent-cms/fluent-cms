using FluentCMS.Cms.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace FluentCMS.Cms.Controllers;

[ApiController]
[Route("[controller]")]
public class PagesController(IPageService pageService) : ControllerBase
{
    [HttpGet("{pageName}")]
    public async Task<ActionResult> Get(string pageName, CancellationToken cancellationToken)
    {
        var queryDictionary = QueryHelpers.ParseQuery(HttpContext.Request.QueryString.Value);
        var htmlContent = await pageService.Get(pageName, queryDictionary, cancellationToken);
        return Content(htmlContent, "text/html");
    }

    [HttpGet("{pageName}/{routerKey}")]
    public async Task<ActionResult> Get(string pageName, string routerKey, CancellationToken cancellationToken)
    {
        var queryDictionary = QueryHelpers.ParseQuery(HttpContext.Request.QueryString.Value);
        var htmlContent = await pageService.GetDetail(pageName, routerKey, queryDictionary, cancellationToken);
        return Content(htmlContent, "text/html");
    }

    [HttpGet]
    public async Task<ActionResult> GetPartial([FromQuery]string token, CancellationToken cancellationToken)
    {
        var htmlContent = await pageService.GetPartial(token, cancellationToken);
        return Content(htmlContent, "text/html");
    }
}