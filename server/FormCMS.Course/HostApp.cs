namespace FormCMS.Course;

public static class HostApp
{
    public static IHost Build(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        var provider = builder.Configuration.GetValue<string>(Constants.DatabaseProvider) ??
                       throw new Exception("DatabaseProvider not found");
        var conn = builder.Configuration.GetConnectionString(provider) ??
                   throw new Exception($"Connection string {provider} not found");

        _ = provider switch
        {
            Constants.Sqlite => builder.Services.AddSqliteCmsWorker(conn,10),
            Constants.Postgres => builder.Services.AddPostgresCmsWorker(conn),
            Constants.SqlServer => builder.Services.AddSqlServerCmsWorker(conn),
            _ => throw new Exception("Database provider not found")
        };
        
        return builder.Build();
    }
}