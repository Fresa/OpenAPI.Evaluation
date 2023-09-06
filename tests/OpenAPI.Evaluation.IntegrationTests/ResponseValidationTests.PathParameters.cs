using System.Net;
using FluentAssertions;
using Xunit.Abstractions;

namespace OpenAPI.Evaluation.IntegrationTests;

public class PathParametersResponseValidationTests : TestSpecification
{
    public PathParametersResponseValidationTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task Given_a_parameter_defined_on_path_when_validating_the_response_should_be_valid()
    {
        var uri = new Uri("v1/user/133e4564-e89b-1ad3-a456-42661aa74000", UriKind.Relative);
        Server.AddGetWithNoResponse(uri);
        var document = LoadOpenApiDocument("path-parameters.yaml");
        using var client = CreateResponseValidatingClient(document);
        var response = await client.GetAsync(uri, Timeout)
            .ConfigureAwait(false);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}