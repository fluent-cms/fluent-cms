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

    [HttpGet("{pageName}/{slug}")]
    public async Task<ActionResult> Get(string pageName, string slug, CancellationToken cancellationToken)
    {
        var htmlContent = await pageService.GetBySlug(pageName,slug, cancellationToken);
        return Content(htmlContent, "text/html");
    }
}