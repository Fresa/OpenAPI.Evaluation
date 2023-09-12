using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using OpenAPI.Evaluation.Collections;

namespace OpenAPI.Evaluation.Specification;

public abstract class Parameter
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
    protected static class Styles
    {
        public const string Matrix = "matrix";
        public const string Label = "label";
        public const string Form = "form";
        public const string Simple = "simple";
        public const string SpaceDelimited = "spaceDelimited";
        public const string PipeDelimited = "pipeDelimited";
        public const string DeepObject = "deepObject";
        public static readonly string[] All = { Matrix, Label, Form, Simple, SpaceDelimited, PipeDelimited, DeepObject };
    }
    protected static class Keys
    {
        internal const string Name = "name";
        internal const string Required = "required";
        internal const string In = "in";
        internal const string Schema = "schema";
        internal const string Description = "description";
        internal const string Content = "content";
        internal const string Style = "style";
        internal const string Deprecated = "deprecated";
        internal const string Explode = "explode";
    }

    private protected Parameter(JsonNodeReader reader)
    {
        _reader = reader;
        Description = ReadDescription();
        Content = ReadContent();
        Schema = ReadSchema();
        AssertSchemaOrContent();
        Style = ReadStyle();
        Deprecated = ReadDeprecated();
        Explode = ReadExplode();
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

    private string? ReadDescription()
    {
        if (!_reader.TryRead(Keys.Description, out var descriptionReader))
            return null;

        Annotations.Add(descriptionReader);
        return descriptionReader.GetValue<string>();
    }

    private Schema? ReadSchema() => _reader.TryRead(Keys.Schema, out var schemaReader) ? Schema.Parse(schemaReader) : null;
    private Content? ReadContent() => _reader.TryRead(Keys.Content, out var contentReader) ? Content.Parse(contentReader) : null;
    protected void AssertLocation(string location)
    {
        if (location != In)
            throw new InvalidOperationException($"Parameter is '{location}', but '{Keys.In}' is '{In}'");
    }
    private void AssertSchemaOrContent()
    {
        if (Schema != null && Content != null)
            throw new InvalidOperationException($"Parameters '{Keys.Schema}' and '{Keys.Content}' cannot both be defined");
        if (Schema == null && Content == null)
            throw new InvalidOperationException($"One of parameters '{Keys.Schema}' or '{Keys.Content}' must be defined");
        if (Content != null && Content.Count != 1) 
            throw new InvalidOperationException($"There must be exactly one media type defined in '{Keys.Content}'");
    }

    private string? ReadStyle()
    {
        if (!_reader.TryRead(Keys.Style, out var styleReader))
            return null;

        Annotations.Add(styleReader);
        return styleReader.GetValue<string>();
    }
    protected void AssertStyle(params string[] validStyles)
    {
        if (Style == null)
            return;
        if (!validStyles.Contains(Style))
            throw new InvalidOperationException($"Style '{Style}' is not valid for parameter location '{In}', valid styles are: {string.Join(", ", validStyles)}");
    }
    private bool ReadDeprecated()
    {
        if (!_reader.TryRead(Keys.Deprecated, out var deprecatedReader))
            return false;

        Annotations.Add(deprecatedReader);
        return deprecatedReader.GetValue<bool>();
    }
    private bool ReadExplode()
    {
        if (!_reader.TryRead(Keys.Explode, out var explodeReader))
            return Style == Styles.Form;

        Annotations.Add(explodeReader);
        return explodeReader.GetValue<bool>();
    }

    protected readonly IDictionary<string, JsonNode?> Annotations = new Dictionary<string, JsonNode?>();

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
    public Schema? Schema { get; private init; }
    public string? Description { get; private init; }
    public Content? Content { get; private init; }
    public string? Style { get; private init; }
    public bool Deprecated { get; private init; }
    public bool Explode { get; protected init; }
}