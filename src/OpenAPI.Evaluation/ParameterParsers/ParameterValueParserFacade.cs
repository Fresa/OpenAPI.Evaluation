using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using OpenAPI.Evaluation.Specification;
using OpenAPI.ParameterStyleParsers.JsonSchema;

namespace OpenAPI.Evaluation.ParameterParsers;

internal sealed class ParameterValueParserFacade(Parameter parameter) : IParameterValueParser
{
    private readonly ParameterStyleParsers.ParameterParsers.ParameterValueParser _parameterValueParser =
        ParameterStyleParsers.ParameterParsers.ParameterValueParser.Create(
                ParameterStyleParsers.Parameter.Parse(
                    name: parameter.Name,
                    style: parameter.Style,
                    location: parameter.In,
                    explode: parameter.Explode,
                    jsonSchema: new JsonSchema202012(parameter.SchemaNode)));
    public string ParameterName => parameter.Name;

    public string ParameterLocation => parameter.In;

    public bool TryParse(string value, out JsonNode? instance, [NotNullWhen(false)] out string? mappingError)
    {

        return _parameterValueParser.TryParse(value, out instance, out mappingError);
    }
}