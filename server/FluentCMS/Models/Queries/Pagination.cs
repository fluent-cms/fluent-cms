using System.Text.Json;
using FluentCMS.Utils.Base64Url;
using SqlKata;

namespace FluentCMS.Models.Queries;

public class Pagination
{
    public int Offset { get; set; }
    public int Limit { get; set; }

    public void Apply(Query? query)
    {
       query?.Offset(Offset)?.Limit(Limit);
    }
}
