using FormCMS.Utils.LocalFileStore;
using FormCMS.Utils.ResultExt;

namespace FormCMS.Cms.Handlers;

public static class FileHandler
{
    public static void MapFileHandlers(this RouteGroupBuilder app)
    {
        app.MapPost($"/", async (
            LocalFileStore store, HttpContext context
        ) => string.Join(",", (await store.Save(context.Request.Form.Files)).Ok()));
    }
}