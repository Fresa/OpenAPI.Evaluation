using System.Net;

namespace OpenAPI.Evaluation.Client;

public class EvaluationHandler : DelegatingHandler
{
    private readonly Specification.OpenAPI _openApi;
    private readonly EvaluationOptions _options;

    public EvaluationHandler(
        Specification.OpenAPI openApi,
        EvaluationOptions options,
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
            evaluationResults = _openApi.Evaluate(request, cancellationToken);
            if (evaluationResults.IsValid == false)
            {
                return CreateEvaluationResponseMessage(new HttpResponseMessage(HttpStatusCode.BadRequest),
                    evaluationResults);
            }
        }

        var response = base.Send(request, cancellationToken);
        if (!_options.EvaluateResponses)
            return response;

        evaluationResults = _openApi.Evaluate(response, cancellationToken);
        return CreateEvaluationResponseMessage(response, evaluationResults);
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
            evaluationResults = await _openApi.EvaluateAsync(request, cancellationToken)
                .ConfigureAwait(false);
            if (evaluationResults.IsValid == false)
            {
                return CreateEvaluationResponseMessage(new HttpResponseMessage(HttpStatusCode.BadRequest),
                    evaluationResults);
            }
        }

        var response = await base.SendAsync(request, cancellationToken)
            .ConfigureAwait(false);
        if (!_options.EvaluateResponses)
            return response;

        evaluationResults = await _openApi.EvaluateAsync(response, cancellationToken)
            .ConfigureAwait(false);
        return CreateEvaluationResponseMessage(response, evaluationResults);
    }
    
    private EvaluationHttpResponseMessage CreateEvaluationResponseMessage(HttpResponseMessage response,
        OpenApiEvaluationResults evaluationResults)
    {
        var evaluationResponse = new EvaluationHttpResponseMessage(response, evaluationResults);
        if (_options.ThrowOnEvaluationFailure)
            evaluationResponse.ThrowIfOpenApiEvaluationIsNotValid();
        return evaluationResponse;
    }
}