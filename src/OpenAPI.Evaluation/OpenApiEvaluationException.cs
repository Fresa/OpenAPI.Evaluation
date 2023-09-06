using System.Text.Json;

namespace OpenAPI.Evaluation;

public class OpenApiEvaluationException : JsonException
{
    internal OpenApiEvaluationException(string message, OpenApiEvaluationResults results) : base(message)
    {
        Results = results;
    }

    public OpenApiEvaluationResults Results { get; }
}