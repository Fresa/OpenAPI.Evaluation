using System.Diagnostics.CodeAnalysis;

namespace OpenAPI.Evaluation.Client;

public static class HttpResponseMessageExtensions
{
    public static bool TryGetOpenApiEvaluationResult(this HttpResponseMessage responseMessage,
        [NotNullWhen(true)] out OpenApiEvaluationResults? evaluationResults)
    {
        if (responseMessage is not EvaluationHttpResponseMessage evaluationHttpResponse)
        {
            evaluationResults = null;
            return false;
        }
        
        evaluationResults = evaluationHttpResponse.EvaluationResults;
        return true;
    }
}