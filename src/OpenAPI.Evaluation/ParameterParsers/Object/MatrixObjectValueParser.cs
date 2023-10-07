using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using Json.Schema;
using OpenAPI.Evaluation.Specification;

namespace OpenAPI.Evaluation.ParameterParsers.Object;

internal sealed class MatrixObjectValueParser : ObjectValueParser
{
    public MatrixObjectValueParser(bool explode, JsonSchema schema) : base(schema, explode)
    {
    }

    public override bool TryParse(
        IReadOnlyCollection<string> values,
        [NotNullWhen(true)] out JsonNode? obj,
        [NotNullWhen(false)] out string? error)
    {
        if (values.Count != 1)
        {
            error = $"Expected one value when parameter style is '{Parameter.Styles.Matrix}'";
            obj = null;
            return false;
        }

        var keyAndValues = values
            .First()
            .Split(';', StringSplitOptions.RemoveEmptyEntries)
            .SelectMany(expression =>
            {
                var valueAndKey = expression.Split('=');
                var key = valueAndKey[0];
                var value = valueAndKey.Length == 1 ? string.Empty : valueAndKey.Last();
                return Explode ? new[] { key, value } : value.Split(',');
            })
            .ToArray();

        return TryGetObjectProperties(keyAndValues, out obj, out error);
    }
}