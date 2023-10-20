using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using Json.Schema;

namespace OpenAPI.Evaluation.ParameterParsers.Object;

internal sealed class DeepObjectValueParser : ObjectValueParser
{
    public DeepObjectValueParser(bool explode, JsonSchema schema) : base(schema, explode)
    {
    }

    public override bool TryParse(
        IReadOnlyCollection<string> values,
        [NotNullWhen(true)] out JsonNode? obj,
        [NotNullWhen(false)] out string? error)
    {
        if (!Explode)
        {
            error = "deep object style without explode is not supported for objects";
            obj = null;
            return false;
        }

        var keyAndValues = values
            .SelectMany(value =>
            {
                var keyAndValue = value
                    .Split('=');
                var key = keyAndValue.First();
                return new[]
                {
                    key[(key.IndexOf('[') + 1)..key.IndexOf(']')],
                    keyAndValue.Last()
                };
            })
            .ToArray();
        return TryGetObjectProperties(keyAndValues, out obj, out error);
    }
}