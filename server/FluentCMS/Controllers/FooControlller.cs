using FluentCMS.Models.Queries;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace FluentCMS.Controllers;
[ApiController]
[Route("api/[controller]")]

public class FooController:ControllerBase
{
    [HttpGet]
    public  ActionResult<dynamic> Get([FromQuery]Pagination? pagination)
    {
        var requestQueryString = HttpContext.Request.QueryString.Value;
        var qs = QueryHelpers.ParseQuery(requestQueryString);
        var sorts = new Sorts(qs);
        var fl = new Filters(qs);
        
        return Ok(new {qs, pagination, sorts, fl});
    }
}