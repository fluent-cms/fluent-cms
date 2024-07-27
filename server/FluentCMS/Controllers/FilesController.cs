using Microsoft.AspNetCore.Mvc;
using Utils.LocalFileStore;

namespace FluentCMS.Controllers;
[ApiController]
[Route("api/[controller]")]
public class FilesController(LocalFileStore store):ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<string>> Post(List<IFormFile> files)
    {
        var paths = await store.Save(files.ToArray());
        return Ok(string.Join(",", paths));
    } 
}