using FluentCMS.Consumer;
using FluentCMS.Utils.Feed;
using FluentCMS.Utils.Nosql;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

var dbInfo = builder.Configuration.GetRequiredSection("DatabaseInfo").Get<DatabaseInfo>()!;
builder.Services.AddSingleton<INosqlDao>(p => new MongoNosqlDao(dbInfo.ConnectionString, dbInfo.Name));

var host = builder.Build();
await Migrate();
host.Run();

return; 
async Task Migrate()
{
    var collections = builder.Configuration.GetSection("Collections").Get<Dictionary<string,string>>();
    if (collections is null)
    {
        return;
    }

    var dao = host.Services.GetService<INosqlDao>()!;
    var logger = host.Services.GetService<ILogger<Feeder>>()!;
    
    foreach (var (key, value) in collections)
    {
        var feed = new Feeder(dao,logger, key, value);
        await feed.GetData();
    }
}

