namespace FluentCMS.Utils.EventStreaming;

public interface IRecordConsumer
{
    void Subscribe();
    RecordMessage? Consume(CancellationToken cancellationToken);
}