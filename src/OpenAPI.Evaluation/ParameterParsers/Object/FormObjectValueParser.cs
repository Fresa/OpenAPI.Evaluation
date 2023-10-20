using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using Json.Schema;

namespace OpenAPI.Evaluation.ParameterParsers.Object;

internal sealed class FormObjectValueParser : ObjectValueParser
{
    public FormObjectValueParser(bool explode, JsonSchema schema) : base(schema, explode)
    {
    }

    public override bool TryParse(
        IReadOnlyCollection<string> values,
        [NotNullWhen(true)] out JsonNode? obj,
        [NotNullWhen(false)] out string? error)
    {
        if (Explode)
        {
            error = "form style with explode not supported for objects as the parameter name cannot be determined";
            obj = null;
            return false;
        }

        if (values.Count != 1)
        {
            error = "Expected one value when parameter doesn't specify explode";
            obj = null;
            return false;
        }

        var keyAndValues = values.First().Split(',');

        return TryGetObjectProperties(keyAndValues, out obj, out error);
    }
}