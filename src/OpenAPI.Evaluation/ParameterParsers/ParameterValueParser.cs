using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using Json.Schema;
using OpenAPI.Evaluation.ParameterParsers.Array;
using OpenAPI.Evaluation.ParameterParsers.Object;
using OpenAPI.Evaluation.ParameterParsers.Primitive;
using OpenAPI.Evaluation.Specification;

namespace OpenAPI.Evaluation.ParameterParsers;

internal sealed class ParameterValueParser : IParameterValueParser
{
    private readonly Parameter _parameter;
    private readonly IValueParser _valueParser;

    private ParameterValueParser(Parameter parameter, IValueParser valueParser)
    {
        _parameter = parameter;
        _valueParser = valueParser;
    }

    internal static ParameterValueParser Create(Parameter parameter, JsonSchema jsonSchema)
    {
        var valueParser = CreateValueParser(parameter, jsonSchema);
        return new ParameterValueParser(parameter, valueParser);
    }
    private static IValueParser CreateValueParser(Parameter parameter, JsonSchema jsonSchema)
    {
        var jsonType = jsonSchema.GetJsonType();

        return jsonType switch
        {
            null => MissingSchemaTypeValueParser.Create(parameter),
            SchemaValueType.String or
                SchemaValueType.Boolean or
                SchemaValueType.Integer or
                SchemaValueType.Number or
                SchemaValueType.Null
                => PrimitiveValueParser.Create(parameter, jsonSchema),
            SchemaValueType.Array => ArrayValueParser.Create(parameter, jsonSchema),
            SchemaValueType.Object => ObjectValueParser.Create(parameter, jsonSchema),
            _ => throw new NotSupportedException($"Json type {Enum.GetName(jsonType.Value)} is not supported")
        };
    }

    public string ParameterName => _parameter.Name;
    public string ParameterLocation => _parameter.In;
    public bool TryParse(string[] values, out JsonNode? instance,
        [NotNullWhen(false)] out string? mappingError) =>
        _valueParser.TryParse(values, out instance, out mappingError);
}