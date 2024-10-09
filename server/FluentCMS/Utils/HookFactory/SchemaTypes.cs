using FluentCMS.Cms.Models;
namespace FluentCMS.Utils.HookFactory;
public record SchemaPreGetAllArgs(string[]? OutSchemaNames) : BaseArgs("*");
public record SchemaPostGetOneArgs(Schema Schema) : BaseArgs("*");
public record SchemaPreSaveArgs(Schema RefSchema ) : BaseArgs("*");
public record SchemaPreDelArgs(int SchemaId) : BaseArgs("*");
