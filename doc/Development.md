
## System Overviews
![overview.png](diagrams%2Foverview.png)
    
## Server
Startup project /fluent-cms/server/FluentCMS/FluentCMS.csproj



- asp.net core
- entity framework core
- sqlkata, it using dapper ORM behind the scene(https://sqlkata.com/)

Both entity framework and sqlkata can abstract query from specific Database dialect, so I extract database access to
another layer, currently fluent-cms support postgres sql, it can easily support SQL Server and MySQL in the future.
