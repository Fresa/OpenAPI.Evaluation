using Json.Schema;
using OpenAPI.Evaluation.ParameterConverters;

namespace OpenAPI.Evaluation;

internal sealed class OpenApiEvaluationOptions
{
    internal required JsonNodeBaseDocument Document { get; init; }
    internal List<IParameterValueConverter> ParameterValueConverters { get; } = new();
    internal required EvaluationOptions JsonSchemaEvaluationOptions { get; init; }
}