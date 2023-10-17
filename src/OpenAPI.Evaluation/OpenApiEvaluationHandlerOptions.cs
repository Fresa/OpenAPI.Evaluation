using OpenAPI.Evaluation.Client;

namespace OpenAPI.Evaluation;

/// <summary>
/// Evaluation options for <see cref="OpenApiEvaluationHandler"/>
/// </summary>
public sealed record OpenApiEvaluationHandlerOptions
{
    /// <summary>
    /// Throws a <see cref="OpenApiEvaluationException"/> on failed request evaluation rather than returning a <see cref="EvaluationHttpResponseMessage"/>
    /// Does nothing if <see cref="EvaluateRequests"/> is set to false.
    /// Default: false
    /// </summary>
    public bool ThrowOnRequestEvaluationFailure { get; init; } = false;
    /// <summary>
    /// Throws a <see cref="OpenApiEvaluationException"/> on failed response evaluation rather than returning a <see cref="EvaluationHttpResponseMessage"/>
    /// Does nothing if <see cref="EvaluateResponses"/> is set to false.
    /// Default: false
    /// </summary>
    public bool ThrowOnResponseEvaluationFailure { get; init; } = false;
    /// <summary>
    /// Evaluates <see cref="HttpRequestMessage"/>
    /// Default: true
    /// </summary>
    public bool EvaluateRequests { get; init; } = true;
    /// <summary>
    /// Evaluates <see cref="HttpResponseMessage"/>
    /// Default: true
    /// </summary>
    public bool EvaluateResponses { get; init; } = true;
}