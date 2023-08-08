using System.Text.Json;

namespace OpenAPI.Validation;

public class OpenApiEvaluationException : JsonException
{
    internal OpenApiEvaluationException(string message, OpenApiEvaluationResults results) : base(message)
    {
        Results = results;
    }

    public OpenApiEvaluationResults Results { get; }
}