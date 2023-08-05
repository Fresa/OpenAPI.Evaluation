using System.Net;
using FluentAssertions;

namespace OpenAPI.Validation.IntegrationTests;

public class ResponseValidationTests : TestSpecification
{
    [Fact]
    public async Task Given_a_user_when_requesting_it_the_response_should_be_valid()
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
}