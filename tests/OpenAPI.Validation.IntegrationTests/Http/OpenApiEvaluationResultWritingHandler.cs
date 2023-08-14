using System.Text.Json;
using System.Text.Json.Serialization;
using OpenAPI.Validation.Client;
using Xunit.Abstractions;

namespace OpenAPI.Validation.IntegrationTests.Http;

internal sealed class OpenApiEvaluationResultWritingHandler : DelegatingHandler
{
    private readonly ITestOutputHelper _testOutputHelper;

    public OpenApiEvaluationResultWritingHandler(ITestOutputHelper testOutputHelper, HttpMessageHandler inner) : base(inner) 
    {
        _testOutputHelper = testOutputHelper;
    }

    protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            var response = base.Send(request, cancellationToken);
            if (response.TryGetOpenApiEvaluationResult(out var result))
                WriteEvaluationResults(result);
            return response;
        }
        catch (OpenApiEvaluationException e)
        {
            WriteEvaluationResults(e.Results);
            throw;
        }
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await base.SendAsync(request, cancellationToken)
                .ConfigureAwait(false);
            if (response.TryGetOpenApiEvaluationResult(out var result))
                WriteEvaluationResults(result);
            return response;
        }
        catch (OpenApiEvaluationException e)
        {
            WriteEvaluationResults(e.Results);
            throw;
        }
    }

    private void WriteEvaluationResults(OpenApiEvaluationResults openApiEvaluationResults)
    {
        //openApiEvaluationResults.IgnoreValidResults();
        var result = JsonSerializer.Serialize(
            openApiEvaluationResults, new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        _testOutputHelper.WriteLine(result);
    }
}