using FluentCMS.Cms.Services;
using FluentCMS.Utils.QueryBuilder;
using Microsoft.AspNetCore.Mvc;

namespace FluentCMS.Cms.Controllers;

[ApiController]
[Route("[controller]")]
public class PagesController(IPageService pageService) : ControllerBase
{
    [HttpGet("{pageName}")]
    public async Task<ActionResult> Get(string pageName, CancellationToken cancellationToken)
    {
        var htmlContent = await pageService.Get(pageName, cancellationToken);
        return Content(htmlContent, "text/html");
    }

    [HttpGet("{pageName}/{routerKey}")]
    public async Task<ActionResult> Get(string pageName, string routerKey, CancellationToken cancellationToken)
    {
        var htmlContent = await pageService.GetByRouterKey(pageName,routerKey, cancellationToken);
        return Content(htmlContent, "text/html");
    }
}