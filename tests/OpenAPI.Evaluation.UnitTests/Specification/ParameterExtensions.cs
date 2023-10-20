using System.Text.Json.Nodes;
using FluentAssertions;
using OpenAPI.Evaluation.Specification;
using OpenAPI.Evaluation.UnitTests.Json;

namespace OpenAPI.Evaluation.UnitTests.Specification;

internal static class ParameterExtensions
{
    internal static void ShouldBeEquivalentTo(this Parameter parameter, JsonNode? node)
    {
        foreach (var (key, value) in node.GetChildren())
        {
            value.Should().NotBeNull();
            switch (key)
            {
                case "name":
                    parameter.Name.Should().Be(value!.GetValue<string>());
                    break;
                case "in":
                    parameter.In.Should().Be(value!.GetValue<string>());
                    break;
                case "required":
                    parameter.Required.Should().Be(value!.GetValue<bool>());
                    break;
                case "schema":
                    parameter.Schema.Should().NotBeNull();
                    break;
                case "description":
                    parameter.Description.Should().Be(value!.GetValue<string>());
                    break;
                case "content":
                    parameter.Content.Should().NotBeNull();
                    break;
                case "style":
                    parameter.Style.Should().Be(value!.GetValue<string>());
                    break;
                case "deprecated":
                    parameter.Deprecated.Should().Be(value!.GetValue<bool>());
                    break;
                case "explode":
                    parameter.Explode.Should().Be(value!.GetValue<bool>());
                    break;
                case "example":
                    parameter.Example.Should().Be(value);
                    break;
                case "examples":
                    parameter.Examples.ShouldBeEquivalentTo(value);
                    break;
            }
        }
    }
}