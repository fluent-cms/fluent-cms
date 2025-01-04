using FluentCMS.DataLink.Types;
using FluentCMS.DataLink.Builders;

namespace FluentCMS.App;

public static class WorkerApp
{
    public static IHost? Build(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        if (builder.Configuration.GetValue<bool>(AppConstants.EnableHostApp) is not true)
        {
            return null;
        }
        
        builder.AddNatsClient(AppConstants.Nats);
        builder.AddMongoDBClient(AppConstants.MongoCms);
        
        var apiLinksArray = builder.Configuration.GetRequiredSection("ApiLinksArray").Get<ApiLinks[]>()!;
        builder.Services.AddNatsMongoLink(apiLinksArray);
        return builder.Build();
    }
}