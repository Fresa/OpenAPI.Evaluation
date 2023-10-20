using System.Text.Json.Nodes;
using FluentAssertions;
using OpenAPI.Evaluation.Specification;
using OpenAPI.Evaluation.UnitTests.Json;

namespace OpenAPI.Evaluation.UnitTests.Specification;

internal static class ExampleExtensions
{
    internal static void ShouldBeEquivalentTo(this Example example, JsonNode? node)
    {
        foreach (var (key, value) in node.GetChildren())
        {
            value.Should().NotBeNull();
            switch (key)
            {
                case "summary":
                    example.Summary.Should().Be(value!.GetValue<string>());
                    break;
                case "description":
                    example.Description.Should().Be(value!.GetValue<string>());
                    break;
                case "value":
                    example.Value.Should().Be(value);
                    break;
                case "externalValue":
                    example.ExternalValue.Should().Be(value!.GetValue<string>());
                    break;
                default:
                    throw new InvalidOperationException($"'{key}' is not a property of an example object");
            }
        }
    }
}