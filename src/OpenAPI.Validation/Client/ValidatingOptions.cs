namespace OpenAPI.Evaluation.Client;

public sealed record ValidatingOptions
{
    public bool ThrowOnEvaluationFailure { get; init; } = false;
    public bool ValidateRequest { get; init; } = true;
    public bool ValidateResponse { get; init; } = true;
}