using System.Text.Json.Nodes;

namespace OpenAPI.Evaluation.Specification;

public sealed partial class Schema
{
    private readonly JsonNodeReader _reader;

    private Schema(JsonNodeReader reader)
    {
        _reader = reader;
    }

    internal static Schema Parse(JsonNodeReader reader)
    {
        return new Schema(reader);
    }

    internal Evaluator GetEvaluator(OpenApiEvaluationContext openApiEvaluationContext) =>
        new(openApiEvaluationContext.Evaluate(_reader));

    public class Evaluator
    {
        private readonly OpenApiEvaluationContext _openApiEvaluationContext;

        internal Evaluator(OpenApiEvaluationContext openApiEvaluationContext)
        {
            _openApiEvaluationContext = openApiEvaluationContext;
        }

        public void Evaluate(JsonNode? instance)
        {
            _openApiEvaluationContext.EvaluateAgainstSchema(instance);
        }

        public void Evaluate(IEnumerable<string?> stringValues)
        {
            _openApiEvaluationContext.EvaluateAgainstSchema(stringValues);
        }
    }
}