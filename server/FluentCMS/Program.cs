using FluentCMS.Auth.DTO;
using FluentCMS.Auth.Services;
using FluentCMS.Cms.DTO;
using NJsonSchema;
using NJsonSchema.CodeGeneration.TypeScript;
using FluentCMS.Core.Descriptors;
using FluentCMS.Utils.JsonUtil;
using NJsonSchema.Generation;

TsGenerator.GenerateCode<Schema>("../../admin-panel/src/cms-client/types/schema.ts");
TsGenerator.GenerateCode<XEntity>("../../admin-panel/src/cms-client/types/schemaExt.ts");
TsGenerator.GenerateCode<ListResponse>("../../admin-panel/src/cms-client/types/listResponse.ts");
TsGenerator.GenerateCode<ListResponseMode>("../../admin-panel/src/cms-client/types/listResponseMode.ts");
TsGenerator.GenerateCode<LookupListResponse>("../../admin-panel/src/cms-client/types/lookupListResponse.ts");

TsGenerator.GenerateCode<Schema>("../../admin-panel/src/auth/types/schema.ts");
TsGenerator.GenerateCode<RoleDto>("../../admin-panel/src/auth/types/roleDto.ts");
TsGenerator.GenerateCode<UserDto>("../../admin-panel/src/auth/types/userDto.ts");
TsGenerator.GenerateCode<ProfileDto>("../../admin-panel/src/auth/types/profileDto.ts");

internal static class TsGenerator
{

    
    internal static void GenerateCode<T>(string fileName)
    {
        var jsonSettings = new SystemTextJsonSchemaGeneratorSettings
        {
            SerializerOptions = JsonOptions.CamelNaming 
        };
        
        var schema = JsonSchema.FromType<T>(jsonSettings);
        var typeScriptGeneratorSettings = new TypeScriptGeneratorSettings ()
        { TypeStyle = TypeScriptTypeStyle.Interface, TypeScriptVersion = 2.0m};
        var generator = new TypeScriptGenerator(schema,typeScriptGeneratorSettings);
        var src = generator.GenerateFile();
        File.WriteAllText(fileName, src);
    }
}