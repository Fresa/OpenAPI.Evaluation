using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;

namespace OpenAPI.Validation.IntegrationTests.Validation;

internal sealed class ResponseValidatingHandler : DelegatingHandler
{
    private readonly OpenApiDocument _openApiDocument;

    public ResponseValidatingHandler(OpenApiDocument openApiDocument, HttpMessageHandler inner) : base(inner)
    {
        _openApiDocument = openApiDocument;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken)
            .ConfigureAwait(false);

        _openApiDocument.TryGetApiOperation(
                response.RequestMessage ?? throw new ArgumentNullException(nameof(response.RequestMessage)),
                out var operation).Should()
            .BeTrue(
                $"{request.Method} {request.RequestUri} should match an operation and path in the OpenAPI specification {_openApiDocument}");
        operation!.TryGetResponseSpecification(response.StatusCode, out var operationResponse).Should().BeTrue();
        var responseEvaluation = await operationResponse!.EvaluateAsync(response, cancellationToken)
            .ConfigureAwait(false);

        if (responseEvaluation.IsValid)
            return response;

        responseEvaluation.IgnoreValidResults();
        throw new JsonException($$"""
            Response evaluation failed: 
            {{JsonSerializer.Serialize(
                responseEvaluation, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                })}}
            """);
    }
}