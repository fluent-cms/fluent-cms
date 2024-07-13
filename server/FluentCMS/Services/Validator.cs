namespace FluentCMS.Services;

public class InvalidParamException(string message) : Exception(message);

public class NullableObjectExceptionBuilder<T>(T? val)
{
    public T ValOrThrow(string message)
    {
        if (val is null)
        {
            throw new InvalidParamException(message);
        }
        return val;
    }
}

public class EmptyStringExceptionBuilder(string? str)
{
    public string ValOrThrow(string message)
    {
        if (string.IsNullOrWhiteSpace(str))
        {
            throw new InvalidParamException(message);
        }

        return str;
    }
}

public class BoolExceptionBuilder(bool val)
{
    public void ThrowTrue(string message)
    {
        if (val)
        {
            throw new InvalidParamException(message);
        }
    }

    public void ThrowFalse(string message)
    {
        if (!val)
        {
            throw new InvalidParamException(message);
        }
    }
}

public static class Val
{
    public  static EmptyStringExceptionBuilder StrNotEmpty(string? str)
    {
        return new EmptyStringExceptionBuilder(str);
    }

    public  static NullableObjectExceptionBuilder<T> NotNull<T>( T? obj)
    {
        return new NullableObjectExceptionBuilder<T>(obj);
    }

    public static BoolExceptionBuilder CheckBool(bool condition)
    {
        return new BoolExceptionBuilder(condition);
    }
}