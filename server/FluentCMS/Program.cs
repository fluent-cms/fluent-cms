using FluentCMS.Cms.DTO;
using FluentCMS.Core.Descriptors;
using NJsonSchema;
using NJsonSchema.CodeGeneration.TypeScript;

TsGenerator.GenerateCms("types.ts");

internal static class TsGenerator
{
    private static readonly TypeScriptGeneratorSettings Settings = new ()
        { TypeStyle = TypeScriptTypeStyle.Interface, TypeScriptVersion = 2.0m };
    public static void GenerateCms(string fileName)
    {
        var code = GenerateCode<Menu>();
        code += GenerateCode<XAttr>();
        code += GenerateCode<Schema>();
        Console.WriteLine(code);
    }

    private static string GenerateCode<T>()
    {
        var schema = JsonSchema.FromType<T>();
        var generator = new TypeScriptGenerator(schema,Settings);
        return generator.GenerateFile();
    }
}