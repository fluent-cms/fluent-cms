using SqlKata;

namespace Utils.QueryBuilder;

public class Pagination
{
    public int Offset { get; set; }
    public int Limit { get; set; }

    public void Apply(Query? query)
    {
       query?.Offset(Offset)?.Limit(Limit);
    }
}
