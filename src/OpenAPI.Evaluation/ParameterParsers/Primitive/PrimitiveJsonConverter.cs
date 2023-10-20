using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using Json.Schema;

namespace OpenAPI.Evaluation.ParameterParsers.Primitive;

internal static class PrimitiveJsonConverter
{
    internal static bool TryConvert(
        string? value,
        SchemaValueType schemaValueType,
        out JsonNode? instance,
        [NotNullWhen(false)] out string? error)
    {
        switch (schemaValueType)
        {
            case SchemaValueType.String:
                instance = JsonValue.Create(value);
                error = null;
                return true;
            case SchemaValueType.Number:
            case SchemaValueType.Boolean:
            case SchemaValueType.Integer:
            case SchemaValueType.Null:
                instance = value == null ? null : JsonNode.Parse(value);
                error = null;
                return true;
            default:
                error = $"Json type {Enum.GetName(schemaValueType)} is not a primitive type";
                instance = null;
                return false;
        }
    }
}