using System.Diagnostics.CodeAnalysis;

namespace OpenAPI.Validation.Client;

public static class HttpResponseMessageExtensions
{
    public static void ThrowIfOpenApiEvaluationIsNotValid(this HttpResponseMessage responseMessage)
    {
        if (responseMessage is not EvaluationHttpResponseMessage evaluationHttpResponse)
            throw new InvalidOperationException("The response message has not been evaluated");

        if (evaluationHttpResponse.EvaluationResults.IsValid)
            return;

        throw new OpenApiEvaluationException("Response failed evaluation", evaluationHttpResponse.EvaluationResults);
    }

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