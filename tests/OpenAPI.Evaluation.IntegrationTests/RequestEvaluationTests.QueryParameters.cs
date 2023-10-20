using System.Net;
using System.Web;
using FluentAssertions;
using Xunit.Abstractions;

namespace OpenAPI.Evaluation.IntegrationTests;

public class QueryParametersRequestEvaluationTests : TestSpecification
{
    public QueryParametersRequestEvaluationTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task Given_a_parameter_defined_on_query_with_content_when_validating_the_response_should_be_valid()
    {
        var query = HttpUtility.ParseQueryString(string.Empty);
        query["user"] = """
            {
              "first-name": "foo",
              "last-name": "bar"
            }
            """;
        
        var uri = new Uri("v1/user/133e4564-e89b-1ad3-a456-42661aa74000?" + query, UriKind.Relative);
        Server.AddGetWithNoResponse(uri);
        var document = LoadOpenApiDocument("parameter-content.yaml");
        using var client = CreateResponseValidatingClient(document);
        var response = await client.GetAsync(uri, Timeout)
            .ConfigureAwait(false);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}