using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using Json.Schema;
using OpenAPI.Evaluation.Specification;

namespace OpenAPI.Evaluation.ParameterParsers.Object;

internal sealed class LabelObjectValueParser : ObjectValueParser
{
    public LabelObjectValueParser(bool explode, JsonSchema schema) : base(schema, explode)
    {
    }

    public override bool TryParse(
        IReadOnlyCollection<string> values,
        [NotNullWhen(true)] out JsonNode? obj,
        [NotNullWhen(false)] out string? error)
    {
        if (values.Count != 1)
        {
            error = $"Expected one value when parameter style is '{Parameter.Styles.Label}'";
            obj = null;
            return false;
        }

        var keyAndValues = values
            .First()
            .Split('.')[1..];
        if (Explode)
        {
            keyAndValues = keyAndValues
                .SelectMany(value => value
                    .Split('='))
                .ToArray();
        }
        return TryGetObjectProperties(keyAndValues, out obj, out error);
    }
}