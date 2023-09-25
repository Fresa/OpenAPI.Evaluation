using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;

namespace OpenAPI.Evaluation.ParameterConverters;

public interface IParameterValueConverter
{
    string ParameterName { get; }
    string ParameterLocation { get; }
    bool TryMap(
        string[] values,
        out JsonNode? instance,
        [NotNullWhen(false)] out string? mappingError);
}