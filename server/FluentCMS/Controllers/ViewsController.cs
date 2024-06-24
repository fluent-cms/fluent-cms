using FluentCMS.Services;
using Microsoft.AspNetCore.Mvc;

namespace FluentCMS.Controllers;
[ApiController]
[Route("api/[controller]")]
public class ViewsController(IViewService viewService): ControllerBase
{
    [HttpGet("{viewName}/{query}")]
    public async Task<ActionResult<IEnumerable<object>>> Get(string viewName)
    {
        var items = await viewService.List(viewName);
        return items is null ? NotFound() : Ok(items);
    }
    
}