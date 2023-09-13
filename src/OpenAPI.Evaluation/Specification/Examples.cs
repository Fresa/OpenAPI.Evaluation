using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace OpenAPI.Evaluation.Specification;

public sealed class Examples : IReadOnlyDictionary<string, Example>
{
    private readonly Dictionary<string, Example> _examples;

    private Examples(JsonNodeReader reader)
    {
        _examples = reader.ReadChildren()
            .ToDictionary(nodeReader => nodeReader.Key, Example.Parse);
    }

    internal static Examples Parse(JsonNodeReader reader) => new(reader);

    public IEnumerator<KeyValuePair<string, Example>> GetEnumerator() => _examples.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public int Count => _examples.Count;
    public bool ContainsKey(string key) => _examples.ContainsKey(key);
    public bool TryGetValue(string key, [MaybeNullWhen(false)] out Example value) => _examples.TryGetValue(key, out value);
    public Example this[string key] => _examples[key];
    public IEnumerable<string> Keys => _examples.Keys;
    public IEnumerable<Example> Values => _examples.Values;
}