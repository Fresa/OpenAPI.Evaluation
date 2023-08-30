using System.Text.Json;
using Json.Schema;

namespace OpenAPI.Evaluation;

internal class JsonSchemaEvaluationException : JsonException
{
    public JsonSchemaEvaluationException(string message, EvaluationResults evaluationResults) : base(message)
    {
        EvaluationResults = new List<EvaluationResults>
        {
            evaluationResults
        };
    }

    internal List<EvaluationResults> EvaluationResults { get; }
}