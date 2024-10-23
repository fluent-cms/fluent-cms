using FluentCMS.Cms.Models;
namespace FluentCMS.Utils.HookFactory;
public record SchemaPreGetAllArgs(string[]? OutSchemaNames) : BaseArgs("");
public record SchemaPostGetOneArgs(Schema Schema) : BaseArgs(Schema.Name);
public record SchemaPreSaveArgs(Schema RefSchema ) : BaseArgs(RefSchema.Name);
public record SchemaPreDelArgs(int SchemaId) : BaseArgs("");
