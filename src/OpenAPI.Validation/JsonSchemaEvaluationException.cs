using System.Text.Json;
using Json.Schema;

namespace OpenAPI.Validation;

internal class JsonSchemaEvaluationException : JsonException
{
    public JsonSchemaEvaluationException(string message, List<EvaluationResults> evaluationResults) : base(message)
    {
        EvaluationResults = evaluationResults;
    }

    public JsonSchemaEvaluationException(string message, EvaluationResults evaluationResults) : base(message)
    {
        EvaluationResults = new List<EvaluationResults>
        {
            evaluationResults
        };
    }

    internal List<EvaluationResults> EvaluationResults { get; }
}