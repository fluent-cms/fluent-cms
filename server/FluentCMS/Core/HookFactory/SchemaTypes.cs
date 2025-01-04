namespace FluentCMS.Core.HookFactory;
public record SchemaPreGetAllArgs(string[]? OutSchemaNames) : BaseArgs("");
public record SchemaPostGetSingleArgs(Descriptors.Schema Schema) : BaseArgs(Schema.Name);
public record SchemaPreSaveArgs(Descriptors.Schema RefSchema ) : BaseArgs(RefSchema.Name);
public record SchemaPostSaveArgs(Descriptors.Schema Schema ) : BaseArgs(Schema.Name);
public record SchemaPreDelArgs(int SchemaId) : BaseArgs("");
