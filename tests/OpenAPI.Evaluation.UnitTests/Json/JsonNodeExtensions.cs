using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using FluentAssertions;
using Json.Pointer;

namespace OpenAPI.Evaluation.UnitTests.Json;

internal static class JsonNodeExtensions
{
    internal static T GetValue<T>(this JsonNode? node, string path)
    {
        var value = node.Evaluate(path);
        return value.GetValue<T>();
    }
    internal static JsonArray GetArray(this JsonNode? node, string path)
    {
        var value = node.Evaluate(path);
        return value.AsArray();
    }

    internal static bool TryGetObject(this JsonNode? node,
        string path,
        [NotNullWhen(true)] out JsonNode? @object) =>
        JsonPointer.Parse(path).TryEvaluate(node, out @object);

    internal static JsonNode GetObject(this JsonNode? node, string path)
    {
        return node.Evaluate(path);
    }

    private static JsonNode Evaluate(this JsonNode? node, string path)
    {
        JsonPointer.Parse(path).TryEvaluate(node, out var value).Should()
            .BeTrue($"because the json node should contain the property {path}");
        value.Should().NotBeNull($"because the property {path} should not be null");
        return value!;
    }
}