using OpenAPI.Evaluation.Specification;

namespace OpenAPI.Evaluation.UnitTests.Specification;

public class ServerTests
{
    [Fact]
    [InlineData()]
    public void Given_server_urls_When_evaluating_urls_They_should_evaluate()
    {
        var server = Server.Parse();
    }

}