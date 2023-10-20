using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using Json.Schema;

namespace OpenAPI.Evaluation.ParameterParsers.Array;

internal sealed class FormArrayValueParser : ArrayValueParser
{
    public FormArrayValueParser(bool explode, JsonSchema schema) : base(schema, explode)
    {
    }

    public override bool TryParse(
        IReadOnlyCollection<string> values,
        [NotNullWhen(true)] out JsonNode? array,
        [NotNullWhen(false)] out string? error)
    {
        if (Explode)
        {
            return TryGetArrayItems(values.ToArray(), out array, out error);
        }

        if (values.Count != 1)
        {
            error = "Expected one value when parameter doesn't specify explode";
            array = null;
            return false;
        }

        var arrayValues = values.First().Split(',');
        return TryGetArrayItems(arrayValues, out array, out error);
    }
}