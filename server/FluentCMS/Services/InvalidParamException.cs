using FluentResults;

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

public class MustTrueExceptionBuilder(bool val)
{
    public void ThrowNotTrue(string message)
    {
        if (!val)
        {
            throw new InvalidParamException(message);
        }
    }
}

public class MustFalseExceptionBuilder(bool val)
{
    public void ThrowNotFalse(string message)
    {
        if (val)
        {
            throw new InvalidParamException(message);
        }
    }
}

public static class InvalidParamExceptionFactory
{
    public  static EmptyStringExceptionBuilder StrNotEmpty(string? str)
    {
        return new EmptyStringExceptionBuilder(str);
    }

    public  static NullableObjectExceptionBuilder<T> NotNull<T>( T? obj)
    {
        return new NullableObjectExceptionBuilder<T>(obj);
    }
    public static MustFalseExceptionBuilder False(bool condition)
    {
        return new MustFalseExceptionBuilder(condition);
    }
    public static MustTrueExceptionBuilder True(bool condition)
    {
        return new MustTrueExceptionBuilder(condition);
    }
    public static void CheckResult(Result? result)
    {
        if (result is not null && result.IsFailed)
        {
            throw new InvalidParamException($"{result.Errors}");
        }
    }
    
    public static T CheckResult<T>(Result<T> result)
    {
        return result switch
        {
            { IsFailed: true } => throw new InvalidParamException($"{result.Errors}"),
            _ => result.Value
        };
    }
}