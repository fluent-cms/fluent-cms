
---
## Aspire Integration
<details> 
<summary> 
A scalable deployment of Fluent CMS involves multiple web application nodes, a Redis server for distributed caching, and a database server, all behind a load balancer.
</summary>

### Architecture Overview

```
                 +------------------+
                 |  Load Balancer   |
                 +------------------+
                          |
        +-----------------+-----------------+
        |                                   |
+------------------+              +------------------+
|    Web App 1     |              |    Web App 2     |
|   +-----------+  |              |   +-----------+  |
|   | Local Cache| |              |   | Local Cache| |
+------------------+              +------------------+
        |                                   |
        |                                   |
        +-----------------+-----------------+
                 |                       |
       +------------------+    +------------------+
       | Database Server  |    |   Redis Server   |
       +------------------+    +------------------+
```

---

### Local Emulation with Aspire and Service Discovery

[Example Web project on GitHub](https://github.com/fluent-cms/fluent-cms/tree/main/server/FluentCMS.Blog)  
[Example Aspire project on GitHub](https://github.com/fluent-cms/fluent-cms/tree/main/server/FluentCMS.Blog.AppHost)  

To emulate the production environment locally, Fluent CMS leverages Aspire. Here's an example setup:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Adding Redis and PostgreSQL services
var redis = builder.AddRedis(name: CmsConstants.Redis);
var db = builder.AddPostgres(CmsConstants.Postgres);

// Configuring the web project with replicas and references
builder.AddProject<Projects.FluentCMS_Blog>(name: "web")
    .WithEnvironment(CmsConstants.DatabaseProvider, CmsConstants.Postgres)
    .WithReference(redis)
    .WithReference(db)
    .WithReplicas(2);

builder.Build().Run();
```

### Benefits:
1. **Simplified Configuration**:  
   No need to manually specify endpoints for the database or Redis servers. Configuration values can be retrieved using:
   ```csharp
   builder.Configuration.GetValue<string>();
   builder.Configuration.GetConnectionString();
   ```
2. **Realistic Testing**:  
   The local environment mirrors the production architecture, ensuring seamless transitions during deployment.

By adopting these caching and deployment strategies, Fluent CMS ensures improved performance, scalability, and ease of configuration.
</details>