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
    [HttpGet("{entityName}")]
    public async Task<ActionResult<ListResult>> List(string entityName, CancellationToken cancellationToken,
        [FromQuery] Pagination pagination)
    {
        var queryDictionary = QueryHelpers.ParseQuery(HttpContext.Request.QueryString.Value);
        return Ok(await entityService.List(entityName, pagination, queryDictionary,cancellationToken));
    }

    [HttpGet("{entityName}/{id}")]
    public async Task<ActionResult<object>> One(string entityName, string id,CancellationToken cancellationToken) =>
        Ok(await entityService.One(entityName, id,cancellationToken));

    [HttpPost("{entityName}/insert")]
    public async Task<ActionResult<int>> Insert(string entityName,CancellationToken cancellationToken, [FromBody] JsonElement item) =>
        Ok(await entityService.Insert(entityName, item,cancellationToken));

    [HttpPost("{entityName}/update")]
    public async Task<ActionResult<int>> Update(string entityName, CancellationToken cancellationToken, [FromBody] JsonElement item) =>
        Ok(await entityService.Update(entityName, item,cancellationToken));

    [HttpPost("{entityName}/delete")]
    public async Task<ActionResult<int>> Delete(string entityName,CancellationToken cancellationToken, [FromBody] JsonElement item) =>
        Ok(await entityService.Delete(entityName, item,cancellationToken));

    [HttpPost("{entityName}/{id}/{attributeName}/delete")]
    public async Task<ActionResult<int>> CrosstableDelete(string entityName, string id, string attributeName, 
        CancellationToken cancellationToken,
        [FromBody] JsonElement[] items) =>
        Ok(await entityService.CrosstableDelete(entityName, id, attributeName, items,cancellationToken));


    [HttpPost("{entityName}/{id}/{attributeName}/save")]
    public async Task<ActionResult<int>> CrosstableSave(string entityName, string id, string attributeName,
        CancellationToken cancellationToken,
        [FromBody] JsonElement[] items) =>
        Ok(await entityService.CrosstableAdd(entityName, id, attributeName, items, cancellationToken));

    [HttpGet("{entityName}/{id}/{attributeName}")]
    public async Task<ActionResult<object>> CrosstableList(
        string entityName, 
        string id, 
        string attributeName, 
        [FromQuery] Pagination pagination,
        CancellationToken cancellationToken,
        [FromQuery] bool exclude) =>
        Ok(await entityService.CrosstableList(entityName, id, attributeName, exclude, pagination, cancellationToken));
}