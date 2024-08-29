using FluentCMS.Cms.Models;
using FluentCMS.Cms.Services;
using Microsoft.AspNetCore.Mvc;

namespace FluentCMS.Cms.Controllers;
[ApiController]
[Route("api/[controller]")]
public class SchemasController(ISchemaService schemaService):ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Schema>>> GetAll(CancellationToken cancellationToken,[FromQuery]string? type )
    {
        return Ok(await schemaService.GetAll(type??"", cancellationToken));
    }
    
    [HttpPost]
    public async Task<ActionResult<Schema>> Post(CancellationToken cancellationToken,[FromBody] Schema dto)
    {
        var item = await schemaService.Save(dto, cancellationToken);
        return  Ok(item);
    }
    [HttpPost("define")]
    public async Task<ActionResult<Schema>> SaveTableDefine( CancellationToken cancellationToken, [FromBody] Schema dto)
    {
        var item = await schemaService.SaveTableDefine(dto, cancellationToken);
        return Ok(item);
    }

    [HttpPost("simple_entity_define")]
    public async Task<ActionResult<Schema>> EnsureSimpleEntity(
        CancellationToken cancellationToken,
        [FromQuery] string entity,
        [FromQuery] string field,
        [FromQuery] string? lookup,
        [FromQuery] string? crosstable
        ) =>
        Ok(await schemaService.AddOrSaveSimpleEntity(entity, field, lookup, crosstable,cancellationToken));
    
    [HttpGet("{name}/define")]
    public async Task<ActionResult<Schema>> GetTableDefine(string name, CancellationToken cancellationToken)
    {
        var item = await schemaService.GetTableDefine(name, cancellationToken);
        return Ok(item);
    }

    [HttpGet("{name}")]
    public async Task<ActionResult<Schema>> GetOne(string name, CancellationToken cancellationToken, [FromQuery] bool? extend)
    {
        return Ok(await schemaService.GetByNameVerify(name, extend?? true, cancellationToken));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await schemaService.Delete(id, cancellationToken);
        return NoContent();
    }

   
}