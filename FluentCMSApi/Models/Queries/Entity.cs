using System.ComponentModel.DataAnnotations;
using Npgsql.Replication.PgOutput.Messages;

namespace FluentCMSApi.models;

public class Entity
{
    public string Name { get; set; } = "";
    public string Title { get; set; } = "";
    public string DataKey { get; set; } = "id";
    public int DefaultPageSize { get; set; } = 20;
    public Column[] Columns { get; set; } = [];
}