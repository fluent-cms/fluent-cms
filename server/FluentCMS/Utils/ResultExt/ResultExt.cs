using FluentResults;

namespace FluentCMS.Utils.ResultExt;

/// <summary>
/// Represents an exception that is deliberately thrown to notify a client about a specific error.
/// </summary>
public class ResultException(string message) : Exception(message);

public static class ResultExt
{
    public static Result BindAction<TValue>(this Result<TValue> res, Action<TValue> action)
    {
        return res.Bind(x =>
        {
            action(x);
            return Result.Ok();
        });
    }
    
    public static bool Try<T>(this Result<T> res, out T val, out List<IError>? err)
    {
        (var ok, _, val,  err) = res;
        return ok;
    }

    public static bool Try(this Result res, out List<IError>? err)
    {
        (var ok, _, err) = res;
        return ok;
    }
    
    /// <summary>
    /// Throws a <see cref="ResultException"/> if the result indicates failure.
    /// Use this method to terminate the current execution flow and return an error message to the client.
    /// Recommended for use in test projects or outer layers of the application.
    /// </summary>
    public static void Ok(this Result? result)
    {
        if (result is not null && result.IsFailed)
        {
            throw new ResultException($"{string.Join("\r\n",result.Errors.Select(e =>e.Message))}");
        }
    }
    
    public static T Ok<T>(this Result<T> result)
    {
        return result switch
        {
            { IsFailed: true } => throw new ResultException($"{string.Join("\r\n",result.Errors.Select(x=>x.Message))}"),
            _ => result.Value
        };
    }
}