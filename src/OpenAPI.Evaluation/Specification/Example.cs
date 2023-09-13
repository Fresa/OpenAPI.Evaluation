using System.Text.Json.Nodes;
using OpenAPI.Evaluation.Collections;

namespace OpenAPI.Evaluation.Specification;

public sealed class Example
{
    private readonly JsonNodeReader _reader;

    private Example(JsonNodeReader reader)
    {
        _reader = reader;
        Summary = ReadSummary();
        Description = ReadDescription();
        Value = ReadValue();
        ExternalValue = ReadExternalValue();
    }

    private readonly IDictionary<string, JsonNode?> _annotations = new Dictionary<string, JsonNode?>();
    internal IReadOnlyDictionary<string, JsonNode?> Annotations => _annotations.AsReadOnly();

    private string? ReadSummary()
    {
        if (!_reader.TryRead("summary", out var summaryReader))
            return null;

        _annotations.Add(summaryReader);
        return summaryReader.GetValue<string>();
    }
    private string? ReadDescription()
    {
        if (!_reader.TryRead("description", out var descriptionReader))
            return null;

        _annotations.Add(descriptionReader);
        return descriptionReader.GetValue<string>();
    }
    private JsonNode? ReadValue()
    {
        if (!_reader.TryRead("value", out var valueReader))
            return null;

        var (key, value) = valueReader;
        _annotations.Add(key, value);
        return value;
    }
    private string? ReadExternalValue()
    {
        if (!_reader.TryRead("externalValue", out var externalValueReader))
            return null;
        
        _annotations.Add(externalValueReader);
        return externalValueReader.GetValue<string>();
    }

    public string? Summary { get; internal init; }
    public string? Description { get; internal init; }
    public JsonNode? Value { get; internal init; }
    public string? ExternalValue { get; internal init; }
    internal static Example Parse(JsonNodeReader reader) => new(reader);
}