using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit.Abstractions;

namespace OpenAPI.Validation.IntegrationTests.Http;

internal sealed class OpenApiEvaluationExceptionWriter : DelegatingHandler
{
    private readonly ITestOutputHelper _testOutputHelper;

    public OpenApiEvaluationExceptionWriter(ITestOutputHelper testOutputHelper, DelegatingHandler inner) : base(inner) 
    {
        _testOutputHelper = testOutputHelper;
    }
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            return await base.SendAsync(request, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OpenApiEvaluationException e)
        {
            var result = JsonSerializer.Serialize(
                e.Results, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });
            _testOutputHelper.WriteLine(result);

            throw;
        }
    }
}