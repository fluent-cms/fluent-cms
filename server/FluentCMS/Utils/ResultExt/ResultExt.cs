using FluentResults;

namespace FluentCMS.Utils.ResultExt;

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
}