namespace FluentCMS.Utils.EventStreaming;

public interface IConsumer
{
    void Subscribe();
    (string, RecordMessage?) Consume(CancellationToken cancellationToken);
    void Dispose();
}