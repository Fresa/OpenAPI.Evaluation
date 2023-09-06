using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using OpenAPI.Evaluation.Collections;

namespace OpenAPI.Evaluation.Specification;

public abstract partial class Parameter
{
    private readonly JsonNodeReader _reader;

    protected static class Location
    {
        public const string Header = "header";
        public const string Path = "path";
        public const string Query = "query";
        public const string Cookie = "cookie";
        public static readonly string[] All = { Header, Path, Query, Cookie };
    }

    protected static class Keys
    {
        internal const string Name = "name";
        internal const string Required = "required";
        internal const string In = "in";
        internal const string Schema = "schema";
        internal const string Description = "description";
    }

    private protected Parameter(JsonNodeReader reader)
    {
        _reader = reader;
    }

    protected bool? ReadRequired()
    {
        if (!_reader.TryRead(Keys.Required, out var requiredReader))
            return null;

        Annotations.Add(requiredReader);
        return requiredReader.GetValue<bool>();
    }

    protected string ReadName()
    {
        var nameReader = _reader.Read(Keys.Name);
        Annotations.Add(nameReader);
        return nameReader.GetValue<string>();
    }

    protected string ReadIn()
    {
        var inReader = _reader.Read(Keys.In);
        Annotations.Add(inReader);
        return inReader.GetValue<string>();
    }

    protected string? ReadDescription()
    {
        if (!_reader.TryRead(Keys.Description, out var descriptionReader))
            return null;

        Annotations.Add(descriptionReader);
        return descriptionReader.GetValue<string>();
    }

    protected Schema? ReadSchema() => _reader.TryRead(Keys.Schema, out var schemaReader) ? Schema.Parse(schemaReader) : null;
    protected void AssertLocation(string location)
    {
        if (location != In)
            throw new InvalidOperationException($"Parameter is '{location}', but '{Keys.In}' is '{In}'");
    }
    protected IDictionary<string, JsonNode?> Annotations = new Dictionary<string, JsonNode?>();

    internal static bool TryParse(JsonNodeReader reader, [NotNullWhen(true)] out Parameter? parameter)
    {
        var @in = reader.Read(Keys.In).GetValue<string>();
        switch (@in)
        {
            case Location.Path:
                parameter = PathParameter.Parse(reader);
                return true;
            case Location.Header:
                var success = HeaderParameter.TryParseRequestHeader(reader, out var headerParameter);
                parameter = headerParameter;
                return success;
            case Location.Query:
                parameter = QueryParameter.Parse(reader);
                return true;
            case Location.Cookie:
                parameter = CookieParameter.Parse(reader);
                return true;
            default:
                throw new InvalidOperationException($"Valid '{Keys.In}' values are {string.Join(", ", Location.All)}, got '{@in}'");
        }
    }

    public abstract string Name { get; protected init; }
    public abstract string In { get; protected init; }
    public abstract bool Required { get; protected init; }
    public abstract Schema? Schema { get; protected init; }
    public abstract string? Description { get; protected init; }
}