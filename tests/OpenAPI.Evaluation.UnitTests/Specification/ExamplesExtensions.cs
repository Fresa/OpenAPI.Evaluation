using System.Text.Json.Nodes;
using FluentAssertions;
using OpenAPI.Evaluation.Specification;
using OpenAPI.Evaluation.UnitTests.Json;

namespace OpenAPI.Evaluation.UnitTests.Specification;

internal static class ExamplesExtensions
{
    internal static void ShouldBeEquivalentTo(this Examples? examples, JsonNode? node)
    {
        foreach (var (key, value) in node.GetChildren())
        {
            examples.Should().ContainKey(key).WhoseValue.ShouldBeEquivalentTo(value);
        }
    }
}