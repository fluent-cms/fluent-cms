using FluentCMS.Consumers;
using FluentCMS.HostAppBuilders;
using FluentCMS.Utils.EventStreaming;

namespace FluentCMS.App;

public static class HostApp
{
    public static IHost Build(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.AddKafkaConsumer<string,string>(AppConstants.Kafka);
        builder.AddMongoDBClient(AppConstants.MongoCms);
        var apiLinksArray = builder.Configuration.GetRequiredSection("ApiLinksArray").Get<ApiLinks[]>()!;
        builder.Services.AddMongoLinker(apiLinksArray);
        return builder.Build();
    }
}