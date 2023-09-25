using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using Json.Schema;
using OpenAPI.Evaluation.Specification;

namespace OpenAPI.Evaluation.ParameterConverters;

internal sealed class SchemaParameterValueConverter : IParameterValueConverter
{
    private readonly JsonSchema _schema;

    public SchemaParameterValueConverter(Parameter parameter, JsonSchema schema)
    {
        _schema = schema;
        ParameterName = parameter.Name;
        ParameterLocation = parameter.In;
    }
    public string ParameterName { get; }
    public string ParameterLocation { get; }
    public bool TryMap(
        string[] values,
        out JsonNode? instance,
        [NotNullWhen(false)] out string? mappingError)
    {
        var jsonType = _schema.GetJsonType();
        if (jsonType == null)
        {
            mappingError = "Schema does not contain a type attribute";
            instance = null;
            return false;
        }

        switch (jsonType)
        {
            case SchemaValueType.String:
                if (!TryGetSingleOrDefaultValue(out var stringValue, out mappingError))
                {
                    instance = null;
                    return false;
                }

                instance = JsonValue.Create(stringValue);
                return true;
            case SchemaValueType.Number:
            case SchemaValueType.Boolean:
            case SchemaValueType.Integer:
            case SchemaValueType.Null:
                if (!TryGetSingleOrDefaultValue(out var value, out mappingError))
                {
                    instance = null;
                    return false;
                }
                instance = value == null ? null : JsonNode.Parse(value);
                return true;
            case SchemaValueType.Object:
            case SchemaValueType.Array:
                throw new NotImplementedException($"Cannot map {jsonType} to json, please register a parameter value mapper");
            default:
                throw new NotSupportedException($"Json type {Enum.GetName(jsonType.Value)} is not supported");
        }

        bool TryGetSingleOrDefaultValue(
            out string? value,
            [NotNullWhen(false)] out string? error)
        {
            if (values.Length > 1)
            {
                error = $"Expected at most one value got {values.Length}";
                value = null;
                return false;
            }

            value = values.FirstOrDefault();
            error = null;
            return true;
        }
    }

}