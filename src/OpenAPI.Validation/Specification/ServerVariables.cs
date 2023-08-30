using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace OpenAPI.Evaluation.Specification;

public sealed class ServerVariables : IReadOnlyDictionary<string, ServerVariable>
{
    private readonly Dictionary<string, ServerVariable> _variables;

    private ServerVariables(JsonNodeReader reader)
    {
        _variables = reader.ReadChildren()
            .ToDictionary(variableReader => variableReader.Key, ServerVariable.Parse);
    }

    internal static ServerVariables Parse(JsonNodeReader reader) => new(reader);

    public IEnumerator<KeyValuePair<string, ServerVariable>> GetEnumerator() => _variables.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public int Count => _variables.Count;
    public bool ContainsKey(string key) => _variables.ContainsKey(key);
    public bool TryGetValue(string key, [MaybeNullWhen(false)] out ServerVariable value) =>
        _variables.TryGetValue(key, out value);
    public ServerVariable this[string key] => _variables[key];
    public IEnumerable<string> Keys => _variables.Keys;
    public IEnumerable<ServerVariable> Values => _variables.Values;
}