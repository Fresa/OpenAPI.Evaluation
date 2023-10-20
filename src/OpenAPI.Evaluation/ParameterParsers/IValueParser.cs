using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;

namespace OpenAPI.Evaluation.ParameterParsers;

internal interface IValueParser
{
    internal bool TryParse(
        IReadOnlyCollection<string> values,
        out JsonNode? instance,
        [NotNullWhen(false)] out string? error);
}