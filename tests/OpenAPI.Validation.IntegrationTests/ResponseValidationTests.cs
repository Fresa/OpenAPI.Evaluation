using System.Net;
using FluentAssertions;
using Xunit.Abstractions;

namespace OpenAPI.Validation.IntegrationTests;

public class ResponseValidationTests : TestSpecification
{
    public ResponseValidationTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task Given_a_user_when_requesting_it_async_the_response_should_be_valid()
    {
        var uri = new Uri("v1/user/133e4564-e89b-1ad3-a456-42661aa74000", UriKind.Relative);
        Server.AddGetResponse(uri,
            """
                {
                    "first-name": "Foo",
                    "last-name": "Bar"
                }
                """);
        var document = LoadOpenApiDocument("test-api.yaml");
        using var client = CreateResponseValidatingClient(document);
        var response = await client.GetAsync(uri, Timeout)
            .ConfigureAwait(false);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public void Given_a_user_when_requesting_it_sync_the_response_should_be_valid()
    {
        var uri = new Uri("v1/user/133e4564-e89b-1ad3-a456-42661aa74000", UriKind.Relative);
        Server.AddGetResponse(uri,
            """
                {
                    "first-name": "Foo",
                    "last-name": "Bar"
                }
                """);
        var document = LoadOpenApiDocument("test-api.yaml");
        using var client = CreateResponseValidatingClient(document);
        var response = client.Send(new HttpRequestMessage(HttpMethod.Get, uri));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}