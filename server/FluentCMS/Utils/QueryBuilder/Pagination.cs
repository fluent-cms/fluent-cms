using SqlKata;

namespace FluentCMS.Utils.QueryBuilder;

public class Pagination
{
    public int Offset { get; set; }
    public int Limit { get; set; }

    public void Apply(SqlKata.Query? query)
    {
       query?.Offset(Offset)?.Limit(Limit);
    }
}
