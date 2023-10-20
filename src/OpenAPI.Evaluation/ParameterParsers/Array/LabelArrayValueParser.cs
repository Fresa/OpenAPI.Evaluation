using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using Json.Schema;
using OpenAPI.Evaluation.Specification;

namespace OpenAPI.Evaluation.ParameterParsers.Array;

internal sealed class LabelArrayValueParser : ArrayValueParser
{
    public LabelArrayValueParser(bool explode, JsonSchema schema) : base(schema, explode)
    {
    }

    public override bool TryParse(
        IReadOnlyCollection<string> values,
        [NotNullWhen(true)] out JsonNode? array,
        [NotNullWhen(false)] out string? error)
    {
        if (values.Count != 1)
        {
            error = $"Expected one value when parameter style is '{Parameter.Styles.Label}'";
            array = null;
            return false;
        }
        var arrayValues = values
            .First()
            .Split('.')[1..];
        return TryGetArrayItems(arrayValues, out array, out error);
    }
}