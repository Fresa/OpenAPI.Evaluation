using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using Json.More;
using Json.Pointer;

namespace OpenAPI.Evaluation;

internal sealed class JsonNodeReader
{
    private readonly JsonNode _root;
    private readonly ConcurrentDictionary<JsonPointer, JsonNodeReader?> _nodeCache = new();
    private static readonly JsonPointer RefPointer = JsonPointer.Create("$ref");

    internal JsonNodeReader(JsonNode root, JsonPointer trail)
    {
        _root = root;
        Trail = trail;
        RootPath = JsonPointer.Parse(root.GetPointerFromRoot());
        Key = RootPath.Segments.LastOrDefault()?.Value ?? string.Empty;
    }

    /// <summary>
    /// The trail leading to this node 
    /// <remarks>This pointer might not be possible to evaluate, it describes the way taken to get to this node</remarks>
    /// </summary>
    internal JsonPointer Trail { get; }
    
    /// <summary>
    /// The absolute path to this node from root
    /// </summary>
    internal JsonPointer RootPath { get; }
    
    /// <summary>
    /// The current json node's key
    /// </summary>
    internal string Key { get; }

    internal JsonNodeReader Read(params PointerSegment[] pointerSegments) =>
        Read(JsonPointer.Create(pointerSegments));
    
    internal JsonNodeReader Read(JsonPointer pointer)
    {
        if (!TryRead(pointer, out var reader))
        {
            throw new InvalidOperationException(
                $"{pointer} does not exist in json {_root}");
        }
        return reader;
    }

    internal bool TryRead(PointerSegment segment, [NotNullWhen(true)] out JsonNodeReader? reader)
        => TryRead(JsonPointer.Create(segment), out reader);
    internal bool TryRead(JsonPointer pointer, [NotNullWhen(true)] out JsonNodeReader? reader)
    {
        reader = _nodeCache.GetOrAdd(pointer, TryRead(pointer));
        return reader != null;
    }

    private JsonNodeReader? TryRead(JsonPointer pointer)
    {
        var reader = ResolveReferences();

        if (!pointer.TryEvaluate(reader._root, out var node) ||
            node == null)
        {
            return null;
        }

        return new JsonNodeReader(node, reader.Trail.Combine(pointer));
    }

    private JsonNodeReader ResolveReferences()
    {
        if (!RefPointer.TryEvaluate(_root, out var referenceNode))
        {
            return this;
        }

        var referencePointerExpression = referenceNode!.GetValue<string>();
        if (!referencePointerExpression.StartsWith("#"))
            throw new InvalidOperationException("Only local (fragment) $ref pointers are supported");

        var referencePointer = JsonPointer.Parse(referencePointerExpression);
        var reader = new JsonNodeReader(_root.Root, Trail.Combine(RefPointer)).Read(
            referencePointer);
        return reader.ResolveReferences();
    }

    internal T GetValue<T>() => _root.GetValue<T>();

    internal IEnumerable<JsonNodeReader> ReadChildren()
    {
        switch (_root)
        {
            case JsonArray array:
                for (var i = 0; i < array.Count; i++)
                {
                    yield return Read(JsonPointer.Create(i));
                }

                break;
            case JsonObject @object:
                foreach (var child in @object)
                {
                    yield return Read(JsonPointer.Create(child.Key));
                }

                break;
            case JsonValue:
                throw new InvalidOperationException("Current node is a value and hence has no children");
            default:
                throw new NotImplementedException($"Nodes of type {_root.GetType()} is currently not supported");
        }
    }
    internal void Deconstruct(out string key, out JsonNode? value)
    {
        key = Key;
        value = _root;
    }
}