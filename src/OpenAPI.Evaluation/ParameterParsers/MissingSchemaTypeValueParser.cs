using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using Json.Schema;
using OpenAPI.Evaluation.ParameterParsers.Array;
using OpenAPI.Evaluation.ParameterParsers.Primitive;
using OpenAPI.Evaluation.Specification;

namespace OpenAPI.Evaluation.ParameterParsers;

internal sealed class MissingSchemaTypeValueParser : IValueParser
{
    private readonly PrimitiveValueParser _primitiveValueParser;
    private readonly ArrayValueParser _arrayValueParser;
    
    private MissingSchemaTypeValueParser(PrimitiveValueParser primitiveValueParser, ArrayValueParser arrayValueParser)
    {
        _primitiveValueParser = primitiveValueParser;
        _arrayValueParser = arrayValueParser;
    }


    internal static MissingSchemaTypeValueParser Create(Parameter parameter)
    {
        var stringSchema = new JsonSchemaBuilder().Type(SchemaValueType.String);
        var primitiveValueParser = PrimitiveValueParser.Create(parameter,
            stringSchema);
        var arrayValueParser = ArrayValueParser.Create(parameter,
            new JsonSchemaBuilder().Type(SchemaValueType.Array).Items(stringSchema));
        return new MissingSchemaTypeValueParser(primitiveValueParser, arrayValueParser);
    }

    public bool TryParse(IReadOnlyCollection<string> values, out JsonNode? instance, 
        [NotNullWhen(false)] out string? error)
    {
        error = null;
        return values.Count switch
        {
            0 => CreateNull(out instance),
            1 => _primitiveValueParser.TryParse(values, out instance, out error),
            _ => _arrayValueParser.TryParse(values, out instance, out error)
        };
    }

    private static bool CreateNull(out JsonNode? instance)
    {
        instance = null;
        return true;
    }
}