add migration
```shell
dotnet ef migrations add first --context PgContext --output-dir Migrations/pg
```

update database
```shell
 dotnet ef database update --context PgContext 
```
 

 
