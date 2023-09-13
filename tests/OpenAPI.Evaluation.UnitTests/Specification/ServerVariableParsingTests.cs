using System.Text.Json.Nodes;
using FluentAssertions;
using Json.Pointer;
using OpenAPI.Evaluation.Specification;
using OpenAPI.Evaluation.UnitTests.Json;

namespace OpenAPI.Evaluation.UnitTests.Specification;

public class ServerVariableParsingTests
{
    [Theory]
    [InlineData("""
        {
            "default": "test"          
        }
        """, false)]
    [InlineData("""
        {
            "Default": "test"          
        }
        """, true)]
    [InlineData("""
        {
            "default": "test",
            "enum": []
        }
        """, true)]
    [InlineData("""
        {
            "default": "test",
            "enum": ["not-test"]
        }
        """, true)]
    [InlineData("""
        {
            "default": "test",
            "enum": ["test"],
            "description": "Testing"
        }
        """, false)]
    public void Given_server_variables_When_parsing_It_should_be_parsed_according_to_spec(
        string serverVariableJson,
        bool shouldThrow)
    {
        var variableJson = JsonNode.Parse(serverVariableJson);
        variableJson.Should().NotBeNull();
        var reader = new JsonNodeReader(variableJson!, JsonPointer.Empty);

        ServerVariable variable;
        try
        {
            variable = ServerVariable.Parse(reader);
        }
        catch
        {
            if (shouldThrow)
                return;
            throw;
        }

        if (variableJson.TryGetObject("/enum", out var enumNode))
        {
            var enums = enumNode.AsArray().Select(node =>
            {
                node.Should().NotBeNull("because enum values should not be null");
                return node!.GetValue<string>();
            }).ToArray();
            variable.Enum.Should().BeEquivalentTo(enums);
        }
        else
        {
            variable.Enum.Should().BeNull();
        }

        var @default = variableJson.ShouldGetObject("/default").GetValue<string>();
        variable.Default.Should().Be(@default);

        if (variableJson.TryGetObject("/description", out var descriptionNode))
        {
            var description = descriptionNode.GetValue<string>();
            variable.Description.Should().Be(description);
        }
        else
        {
            variable.Description.Should().BeNull();
        }
    }
}