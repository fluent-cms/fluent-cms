namespace Utils.QueryBuilder;
public class QueryException(string message) : Exception(message);
public class NullQueryBuilder<T>(T? val)
{
    public T ValueOrThrow(string message)
    {
        if (val is null)
        {
            throw new QueryException(message);
        }
        return val;
    }
}

public class EmptyStringExceptionBuilder(string? str)
{
    public string ValueOrThrow(string message)
    {
        if (string.IsNullOrWhiteSpace(str))
        {
            throw new QueryException(message);
        }

        return str;
    }
}

public static class QueryExceptionChecker
{
    public  static EmptyStringExceptionBuilder StrNotEmpty(string? str)
    {
        return new EmptyStringExceptionBuilder(str);
    }
    public  static NullQueryBuilder<T> NotNull<T>( T? obj)
    {
        return new NullQueryBuilder<T>(obj);
    }
}