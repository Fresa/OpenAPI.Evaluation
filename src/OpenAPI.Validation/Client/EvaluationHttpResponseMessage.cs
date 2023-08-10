namespace OpenAPI.Validation.Client;

public sealed class EvaluationHttpResponseMessage : HttpResponseMessage
{
    internal EvaluationHttpResponseMessage(HttpResponseMessage response, OpenApiEvaluationResults evaluationResults)
    {
        EvaluationResults = evaluationResults;

        Content = response.Content;
        ReasonPhrase = response.ReasonPhrase;
        RequestMessage = response.RequestMessage;
        StatusCode = response.StatusCode;
        Version = response.Version;
        foreach (var (key, value) in response.Headers)
        {
            Headers.Add(key, value);
        }
        foreach (var (key, value) in response.TrailingHeaders)
        {
            TrailingHeaders.Add(key, value);
        }
    }

    public OpenApiEvaluationResults EvaluationResults { get; }
}