namespace FluentCMS.Utils.EventStreaming;

public interface IStringMessageConsumer
{
    Task Subscribe(Func<string, Task> handler, CancellationToken ct);
}