using Json.Schema;
using OpenAPI.Evaluation.ParameterParsers;

namespace OpenAPI.Evaluation;

internal sealed class OpenApiEvaluationOptions
{
    internal required JsonNodeBaseDocument Document { get; init; }
    internal List<IParameterValueParser> ParameterValueParsers { get; } = new();
    internal required Json.Schema.EvaluationOptions JsonSchemaEvaluationOptions { get; init; }
}