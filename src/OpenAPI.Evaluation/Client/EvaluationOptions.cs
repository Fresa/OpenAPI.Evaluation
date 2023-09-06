namespace OpenAPI.Evaluation.Client;

/// <summary>
/// Evaluation options
/// </summary>
public sealed record EvaluationOptions
{
    /// <summary>
    /// Throws a <see cref="OpenApiEvaluationException"/> on failed evaluations rather than returning a <see cref="EvaluationHttpResponseMessage"/>
    /// </summary>
    public bool ThrowOnEvaluationFailure { get; init; } = false;
    /// <summary>
    /// Evaluates <see cref="HttpRequestMessage"/>
    /// </summary>
    public bool EvaluateRequests { get; init; } = true;
    /// <summary>
    /// Evaluates <see cref="HttpResponseMessage"/>
    /// </summary>
    public bool EvaluateResponses { get; init; } = true;
}