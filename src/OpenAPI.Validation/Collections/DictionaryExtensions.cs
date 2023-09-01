using System.Text.Json.Nodes;

namespace OpenAPI.Evaluation.Collections;

internal static class DictionaryExtensions
{
    public static void Add(this IDictionary<string, JsonNode?> dictionary, JsonNodeReader reader)
    {
        var (key, value) = reader;
        dictionary.Add(key, value);
    }
}