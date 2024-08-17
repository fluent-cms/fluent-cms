using FluentCMS.Utils.HookFactory;
using FluentCMS.Utils.Message;
using FluentCMS.Utils.QueryBuilder;

namespace FluentCMS.Utils.MessageProducer;

public class ProducerHookRegister(HookRegistry registry, IMessageProducer messageProducer, ILogger<ProducerHookRegister> logger)
{
    public void RegisterMessageProducer(string entityName)
    {
        registry.AddHooks(entityName, [Occasion.AfterInsert], (RecordMeta meta,Record record) =>
        {
            logger.LogInformation("Registered Entity Inserted Message Producer Hook");
            messageProducer.ProduceRecord(Topics.EntityCreated, meta, record);
        });
        
        registry.AddHooks(entityName, [Occasion.AfterUpdate], (RecordMeta meta,Record record) =>
        {
            logger.LogInformation("Registered Entity Updated Message Producer Hook");
            messageProducer.ProduceRecord(Topics.EntityUpdated, meta, record);
        });
        registry.AddHooks(entityName, [Occasion.AfterDelete], (RecordMeta meta,Record record) =>
        {
            logger.LogInformation("Registered Entity Deleted Message Producer Hook");
            messageProducer.ProduceRecord(Topics.EntityDeleted, meta, record);
        });
    }
    
}