using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;

namespace OpenAPI.Evaluation.ParameterParsers;

public interface IParameterValueParser 
{
    string ParameterName { get; }
    string ParameterLocation { get; }
    bool TryParse(
        string values,
        out JsonNode? instance,
        [NotNullWhen(false)] out string? mappingError);
}