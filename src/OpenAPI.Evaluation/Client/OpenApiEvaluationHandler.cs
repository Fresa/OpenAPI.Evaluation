using System.Net;

namespace OpenAPI.Evaluation.Client;

public class OpenApiEvaluationHandler : DelegatingHandler
{
    private readonly Specification.OpenAPI _openApi;
    private readonly OpenApiEvaluationHandlerOptions _options;

    public OpenApiEvaluationHandler(
        Specification.OpenAPI openApi,
        OpenApiEvaluationHandlerOptions options,
        HttpMessageHandler inner) : base(inner)
    {
        _openApi = openApi;
        _options = options;
    }

    /// <summary>
    /// Sends a request and validates the response according to the OpenAPI document
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns><see cref="EvaluationHttpResponseMessage"/>The response message and evaluation results</returns>
    /// <exception cref="InvalidOperationException">Thrown when the request doesn't match with any known api operation in the OpenAPI spec</exception>
    /// <exception cref="OpenApiEvaluationException">Thrown when the evaluation result is not valid and throwOnEvaluationFailure has been set</exception>
    protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        OpenApiEvaluationResults? evaluationResults;

        if (_options.EvaluateRequests)
        {
            evaluationResults = _openApi.EvaluateRequest(request, cancellationToken);
            if (!evaluationResults.IsValid)
            {
                return _options.ThrowOnRequestEvaluationFailure
                    ? throw CreateOpenApiEvaluationException(evaluationResults)
                    : CreateEvaluationResponseMessage(new HttpResponseMessage(HttpStatusCode.BadRequest),
                        evaluationResults);
            }
        }

        var response = base.Send(request, cancellationToken);
        if (!_options.EvaluateResponses)
            return response;

        evaluationResults = _openApi.EvaluateResponse(response, cancellationToken);
        if (!evaluationResults.IsValid)
        {
            return _options.ThrowOnResponseEvaluationFailure
                ? throw CreateOpenApiEvaluationException(evaluationResults)
                : CreateEvaluationResponseMessage(response, evaluationResults);
        }
        return response;
    }

    /// <summary>
    /// Sends a request and validates the response according to the OpenAPI document
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns><see cref="EvaluationHttpResponseMessage"/>The response message and evaluation results</returns>
    /// <exception cref="InvalidOperationException">Thrown when the request doesn't match with any known api operation in the OpenAPI spec</exception>
    /// <exception cref="OpenApiEvaluationException">Thrown when the evaluation result is not valid and throwOnEvaluationFailure has been set</exception>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        OpenApiEvaluationResults? evaluationResults;

        if (_options.EvaluateRequests)
        {
            evaluationResults = await _openApi.EvaluateRequestAsync(request, cancellationToken)
                .ConfigureAwait(false);
            if (!evaluationResults.IsValid)
            {
                return _options.ThrowOnRequestEvaluationFailure
                    ? throw CreateOpenApiEvaluationException(evaluationResults)
                    : CreateEvaluationResponseMessage(new HttpResponseMessage(HttpStatusCode.BadRequest),
                        evaluationResults);
            }
        }

        var response = await base.SendAsync(request, cancellationToken)
            .ConfigureAwait(false);
        if (!_options.EvaluateResponses)
            return response;

        evaluationResults = await _openApi.EvaluateResponseAsync(response, cancellationToken)
            .ConfigureAwait(false);
        if (!evaluationResults.IsValid)
        {
            return _options.ThrowOnResponseEvaluationFailure
                ? throw CreateOpenApiEvaluationException(evaluationResults)
                : CreateEvaluationResponseMessage(response, evaluationResults);
        }
        return response;
    }

    private static OpenApiEvaluationException CreateOpenApiEvaluationException(OpenApiEvaluationResults evaluationResults) =>
        new("Evaluation failed", evaluationResults);

    private EvaluationHttpResponseMessage CreateEvaluationResponseMessage(HttpResponseMessage response,
        OpenApiEvaluationResults evaluationResults)
    {
        var evaluationResponse = new EvaluationHttpResponseMessage(response, evaluationResults);
        return evaluationResponse;
    }
}