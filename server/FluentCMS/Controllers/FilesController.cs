using FluentCMS.Utils.File;
using Microsoft.AspNetCore.Mvc;

namespace FluentCMS.Controllers;
[ApiController]
[Route("api/[controller]")]
public class FilesController(FileUtl fileUtl):ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<string>> Post(List<IFormFile> files)
    {
        var paths = await fileUtl.Save(files.ToArray());
        return Ok(string.Join(",", paths));
    } 
}