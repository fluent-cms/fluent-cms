using System.Text.Json;
using FluentResults;

namespace FluentCMS.Core.Descriptors;
public static class  SchemaType
{
    public const string Menu = "menu";
    public const string Entity = "entity";
    public const string Query = "query";
    public const string Page = "page";
}


public sealed record Settings(Entity? Entity = null, Query? Query =null, Menu? Menu =null, Page? Page = null);
public record Schema(string Name, string Type, Settings Settings, int Id = 0, string CreatedBy ="");

public static class SchemaFields
{
    public const string Id = "id";
    public const string Name = "name";
    public const string Type = "type";
    public const string Settings = "settings";
    public const string Deleted = "deleted";
    public const string CreatedBy = "created_by"; 
    public static readonly string[] Fields = [Id, Name, Type, Settings, CreatedBy];
}

public static class SchemaHelper
{
    public const string TableName = "__schemas";

    public static SqlKata.Query BaseQuery(string[]fields)
    {
        return new SqlKata.Query(TableName)
            .Select(fields)
            .Where(SchemaFields.Deleted, false);
    }

    public static SqlKata.Query BaseQuery()
    {
        return BaseQuery(SchemaFields.Fields);
    }

    public static SqlKata.Query SoftDelete(int id)
    {
        return new SqlKata.Query(TableName)
            .Where(SchemaFields.Id, id).AsUpdate([SchemaFields.Deleted], [true]);
    } 
    
    public static SqlKata.Query Save(this Schema dto)
    {
        if (dto.Id == 0)
        {
            var record = new Dictionary<string, object>
            {
                { SchemaFields.Name, dto.Name },
                { SchemaFields.Type, dto.Type },
                { SchemaFields.Settings, JsonSerializer.Serialize(dto.Settings) },
                { SchemaFields.CreatedBy, dto.CreatedBy }
            };

            return new SqlKata.Query(TableName).AsInsert(record, true);
        }

        var query = new SqlKata.Query(TableName)
            .Where(SchemaFields.Id, dto.Id)
            .AsUpdate(
                [SchemaFields.Name, SchemaFields.Type, SchemaFields.Settings],
                [dto.Name, dto.Type, JsonSerializer.Serialize(dto.Settings)]
            );
        return query;

    }
    
    public static Result<Schema> ParseSchema(Record? record)
    {
        if (record is null)
        {
            return Result.Fail("Can not parse schema, input record is null");
        }

        return Result.Try(() =>
        {
            record = record.ToDictionary(pair => pair.Key.ToLower(), pair => pair.Value);
            var s = JsonSerializer.Deserialize<Settings>((string)record[SchemaFields.Settings]);
            return new Schema
            (
                Name: (string)record[SchemaFields.Name],
                Type: (string)record[SchemaFields.Type],
                Settings: s!,
                CreatedBy: (string)record[SchemaFields.CreatedBy],
                Id: record[SchemaFields.Id] switch
                {
                    int val => val,
                    long val => (int)val,
                    _ => 0
                }
            );
        });
    }
}