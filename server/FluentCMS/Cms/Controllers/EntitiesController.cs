using System.Text.Json;
using FluentCMS.Cms.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using FluentCMS.Utils.QueryBuilder;

namespace FluentCMS.Cms.Controllers;
[ApiController]
[Route("api/[controller]")]
public class EntitiesController(IEntityService entityService) : ControllerBase
{
    [HttpGet("{name}")]
    public async Task<ActionResult<ListResult>> List(string name, [FromQuery] Pagination pagination, CancellationToken ct
        )
    {
        var queryDictionary = QueryHelpers.ParseQuery(HttpContext.Request.QueryString.Value);
        return Ok(await entityService.List(name, pagination, queryDictionary,ct));
    }

    [HttpGet("{name}/{id}")]
    public async Task<ActionResult<object>> One(string name, string id,CancellationToken ct) =>
        Ok(await entityService.One(name, id,ct));

    [HttpPost("{name}/insert")]
    public async Task<ActionResult<int>> Insert(string name, [FromBody] JsonElement ele,CancellationToken ct)
    {
        var item = await entityService.Insert(name, ele, ct);
        return Ok(item);
    }

    [HttpPost("{name}/update")]
    public async Task<ActionResult<int>> Update(string name, CancellationToken ct, [FromBody] JsonElement ele) =>
        Ok(await entityService.Update(name, ele,ct));

    [HttpPost("{name}/delete")]
    public async Task<ActionResult<int>> Delete(string name, [FromBody] JsonElement ele,CancellationToken ct) =>
        Ok(await entityService.Delete(name, ele,ct));

    [HttpPost("{entityName}/{id}/{attributeName}/delete")]
    public async Task<ActionResult<int>> CrosstableDelete(string entityName, string id, string attributeName, 
        CancellationToken cancellationToken,
        [FromBody] JsonElement[] items) =>
        Ok(await entityService.CrosstableDelete(entityName, id, attributeName, items,cancellationToken));


    [HttpPost("{name}/{id}/{attr}/save")]
    public async Task<ActionResult<int>> CrosstableSave(string name, string id, string attr,
        [FromBody] JsonElement[] elements,
        CancellationToken ct
        ) =>
        Ok(await entityService.CrosstableAdd(name, id, attr, elements, ct));

    [HttpGet("{name}/{id}/{attr}")]
    public async Task<ActionResult<object>> CrosstableList(
        string name, 
        string id, 
        string attr, 
        [FromQuery] Pagination pagination,
        [FromQuery] bool exclude,
        CancellationToken ct
        ) {
        var queryDictionary = QueryHelpers.ParseQuery(HttpContext.Request.QueryString.Value);
        return Ok(await entityService.CrosstableList(name, id, attr, exclude, queryDictionary, pagination, ct));
    }
}