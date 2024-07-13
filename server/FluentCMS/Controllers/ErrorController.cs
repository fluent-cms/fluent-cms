using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace FluentCMS.Controllers;

public class ErrorController:ControllerBase
{
    [Route("/error-development")]
    [ApiExplorerSettings(IgnoreApi = true)]

    public IActionResult HandleErrorDevelopment(
        [FromServices] IHostEnvironment hostEnvironment)
    {
        if (!hostEnvironment.IsDevelopment())
        {
            return NotFound();
        }

        var ex = HttpContext.Features.Get<IExceptionHandlerFeature>()?.Error!;

        return ex is Services.InvalidParamException ? 
            Problem(title: ex.Message, detail:ex.StackTrace, statusCode:400)
            : Problem( detail: ex.StackTrace, title: ex.Message);
    }

    [Route("/error")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public IActionResult HandleError() {
        var ex = HttpContext.Features.Get<IExceptionHandlerFeature>()?.Error!;
        return ex is Services.InvalidParamException ? 
            Problem(title: ex.Message, statusCode:400)
            : Problem();
    }
}