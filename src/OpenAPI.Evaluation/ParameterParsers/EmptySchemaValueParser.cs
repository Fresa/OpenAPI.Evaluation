using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;

namespace OpenAPI.Evaluation.ParameterParsers;

internal sealed class EmptySchemaValueParser : IValueParser
{
    public bool TryParse(IReadOnlyCollection<string> values, out JsonNode? instance, 
        [NotNullWhen(false)] out string? error)
    {
        error = null;
        instance = values.Count switch
        {
            0 => new JsonObject(),
            1 => JsonValue.Create(values.First()),
            _ => new JsonArray(values.Select(item => (JsonNode?)JsonValue.Create(item)).ToArray())
        };
        return true;
    }
}