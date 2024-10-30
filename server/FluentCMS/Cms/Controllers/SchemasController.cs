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
    public async Task<ActionResult<IEnumerable<Schema>>> GetAll([FromQuery] string? type, CancellationToken cancellationToken)
    {
        return Ok(await schemaService.GetAll(type ?? "", cancellationToken));
    }

    // handle both save/create
    [HttpPost]
    public async Task<ActionResult<Schema>> Post( [FromBody] Schema dto, CancellationToken cancellationToken)
    {
        return dto.Type switch
        {
            SchemaType.Query => await querySchemaService.Save(dto, cancellationToken),//query need extra check
            _ => await schemaService.Save(dto, cancellationToken)
        };
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Schema>> GetOne(int id, CancellationToken cancellationToken)
    {
        var schema = await schemaService.GetByIdWithTrigger(id, cancellationToken);
        if (schema is null)
        {
            return NotFound($"can not find schema {id}");
        }

        return Ok(schema);
    }
    
    // for default schema like 'top-menu-bar', it convenient to get by name directly
    [HttpGet("name/{name}")]
    public async Task<ActionResult<Schema>> GetOne(string name,[FromQuery] string type, CancellationToken cancellationToken)
    {
        var schema = await schemaService.GetByNameDefault(name, type, cancellationToken);
        if (schema is null)
        {
            return NotFound($"can not find schema {name} of type {type}");
        }

        return Ok(schema);
    }
    
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await schemaService.Delete(id, cancellationToken);
        return NoContent();
    }
 
    [HttpPost("entity/define")]
    public async Task<ActionResult<Schema>> SaveTableDefine(CancellationToken cancellationToken, [FromBody] Schema dto)
    {
        var item = await entitySchemaService.SaveTableDefine(dto, cancellationToken);
        return Ok(item);
    }

    [HttpGet("entity/{name}/define")]
    public async Task<ActionResult<Schema>> GetTableDefine(string name, CancellationToken cancellationToken)
    {
        var item = await entitySchemaService.GetTableDefine(name, cancellationToken);
        return Ok(item);
    }

    /// for admin panel
    [HttpGet("entity/{name}")]
    public async Task<ActionResult<LoadedEntity>> GetOne(string name, CancellationToken cancellationToken)
    {
        var schema =
            InvalidParamExceptionFactory.CheckResult(
                await entitySchemaService.GetLoadedEntity(name,  cancellationToken));
        return Ok(schema);
    }

    [HttpPost("entity/add_or_update")]
    public async Task<ActionResult<Schema>> AddOrUpdate(
        [FromBody] Entity dto,
        CancellationToken cancellationToken
    ) =>
        Ok(await entitySchemaService.AddOrUpdate(dto, cancellationToken));
}