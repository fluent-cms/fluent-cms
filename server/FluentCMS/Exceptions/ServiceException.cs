using FluentResults;

namespace FluentCMS.Exceptions;

public class ServiceException(string message) : Exception(message);

public static class ResultExt
{
    public static void Ok(this Result? result)
    {
        if (result is not null && result.IsFailed)
        {
            throw new ServiceException($"{string.Join("\r\n",result.Errors.Select(e =>e.Message))}");
        }
    }
    
    public static T Ok<T>(this Result<T> result)
    {
        return result switch
        {
            { IsFailed: true } => throw new ServiceException($"{string.Join("\r\n",result.Errors.Select(x=>x.Message))}"),
            _ => result.Value
        };
    }
}