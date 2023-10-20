using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using FluentAssertions;
using Json.Pointer;

namespace OpenAPI.Evaluation.UnitTests.Json;

internal static class JsonNodeExtensions
{
    internal static T ShouldGetValue<T>(this JsonNode? node, string path)
    {
        var value = node.Evaluate(path);
        return value.GetValue<T>();
    }
    internal static bool TryGetValue<T>(this JsonNode? node, string path, [NotNullWhen(true)] out T? value) 
    {
        if (node.TryGetObject(path, out var @object))
        {
            value = @object.GetValue<T>();
            return value != null;
        }

        value = default;
        return false;
    }
    internal static JsonArray ShouldGetArray(this JsonNode? node, string path)
    {
        var value = node.Evaluate(path);
        return value.AsArray();
    }

    internal static bool TryGetObject(this JsonNode? node,
        string path,
        [NotNullWhen(true)] out JsonNode? @object) =>
        JsonPointer.Parse(path).TryEvaluate(node, out @object);

    internal static JsonNode ShouldGetObject(this JsonNode? node, string path)
    {
        return node.Evaluate(path);
    }

    internal static IEnumerable<(string Key, JsonNode? Value)> GetChildren(this JsonNode? node)
    {
        if (node == null)
            return Enumerable.Empty<(string Key, JsonNode? Value)>();

        return new JsonNodeReader(node, JsonPointer.Empty).ReadChildren().Select(reader =>
        {
            reader.Deconstruct(out var key, out var value);
            return (key, value);
        });
    }

    private static JsonNode Evaluate(this JsonNode? node, string path)
    {
        JsonPointer.Parse(path).TryEvaluate(node, out var value).Should()
            .BeTrue($"because the json node should contain the property {path}");
        value.Should().NotBeNull($"because the property {path} should not be null");
        return value!;
    }
}