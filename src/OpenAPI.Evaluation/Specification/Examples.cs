using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;

namespace OpenAPI.Evaluation.Specification;

public sealed class Examples : IReadOnlyDictionary<string, Example>
{
    private readonly Dictionary<string, Example> _examples;

    private Examples(JsonNodeReader reader)
    {
        _examples = reader.ReadChildren()
            .ToDictionary(nodeReader => nodeReader.Key, nodeReader =>
            {
                var example = Example.Parse(nodeReader);
                _annotations.Add(nodeReader.Key, new JsonObject(example.Annotations));
                return example;
            });
    }

    internal static Examples Parse(JsonNodeReader reader) => new(reader);

    private readonly IDictionary<string, JsonNode?> _annotations =
        new Dictionary<string, JsonNode?>();
    internal IReadOnlyDictionary<string, JsonNode?> Annotations => _annotations.AsReadOnly();

    public IEnumerator<KeyValuePair<string, Example>> GetEnumerator() => _examples.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public int Count => _examples.Count;
    public bool ContainsKey(string key) => _examples.ContainsKey(key);
    public bool TryGetValue(string key, [MaybeNullWhen(false)] out Example value) => _examples.TryGetValue(key, out value);
    public Example this[string key] => _examples[key];
    public IEnumerable<string> Keys => _examples.Keys;
    public IEnumerable<Example> Values => _examples.Values;
}