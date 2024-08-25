namespace FluentCMS.Utils.EventStreaming;

public interface IConsumer
{
    void Subscribe();
    Task<RecordMessage?> Consume(CancellationToken cancellationToken);
    void Dispose();
}