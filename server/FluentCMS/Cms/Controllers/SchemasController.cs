using FluentCMS.Cms.Models;
using FluentCMS.Cms.Services;
using FluentCMS.Services;
using FluentCMS.Utils.QueryBuilder;
using Microsoft.AspNetCore.Mvc;

namespace FluentCMS.Cms.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SchemasController(
    ISchemaService schemaService,
    IEntitySchemaService entitySchemaService,
    IQuerySchemaService querySchemaService
) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Schema>>> GetAll([FromQuery] string? type, CancellationToken token)
    {
        return Ok(await schemaService.AllWithAction(type ?? "", token));
    }

    // handle both save/create
    [HttpPost]
    public async Task<ActionResult<Schema>> Post([FromBody] Schema dto, CancellationToken token)
    {
        return dto.Type switch
        {
            SchemaType.Query => await querySchemaService.Save(dto, token), //query need extra check
            _ => await schemaService.SaveWithAction(dto, token)
        };
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Schema>> GetOne(int id, CancellationToken token)
    {
        var schema = await schemaService.ByIdWithAction(id, token);
        if (schema is null)
        {
            return NotFound($"can not find schema {id}");
        }

        return Ok(schema);
    }

    // for default schema like 'top-menu-bar', it convenient to get by name directly
    [HttpGet("name/{name}")]
    public async Task<ActionResult<Schema>> GetOne(string name, [FromQuery] string type, CancellationToken token)
    {
        var schema = await schemaService.GetByNameDefault(name, type, token);
        if (schema is null)
        {
            return NotFound($"can not find schema {name} of type {type}");
        }

        return Ok(schema);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id, CancellationToken token)
    {
        var schema = await schemaService.ById(id, token);
        if (schema?.Type == SchemaType.Query)
        {
            await querySchemaService.Delete(schema, token);
        }
        else
        {
            await schemaService.Delete(id, token);
        }

        return NoContent();
    }

    [HttpPost("entity/define")]
    public async Task<ActionResult<Schema>> SaveTableDefine([FromBody] Schema dto, CancellationToken token)
    {
        var item = await entitySchemaService.SaveTableDefine(dto, token);
        return Ok(item);
    }

    [HttpGet("entity/{name}/define")]
    public async Task<ActionResult<Schema>> GetTableDefine(string name, CancellationToken token)
    {
        var item = await entitySchemaService.GetTableDefine(name, token);
        return Ok(item);
    }

    /// for admin panel
    [HttpGet("entity/{name}")]
    public async Task<ActionResult<LoadedEntity>> GetOne(string name, CancellationToken token)
    {
        var schema =
            InvalidParamExceptionFactory.Ok(
                await entitySchemaService.GetLoadedEntity(name, token));
        return Ok(schema);
    }

    [HttpPost("entity/add_or_update")]
    public async Task<ActionResult<Schema>> AddOrUpdate(
        [FromBody] Entity dto,
        CancellationToken token
    ) =>
        Ok(await entitySchemaService.AddOrUpdateByName(dto, token));
}