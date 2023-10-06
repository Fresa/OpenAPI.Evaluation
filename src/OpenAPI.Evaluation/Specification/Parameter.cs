using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using Json.Schema;
using OpenAPI.Evaluation.Collections;
using OpenAPI.Evaluation.Http;
using OpenAPI.Evaluation.ParameterConverters;

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
    internal static class Styles
    {
        public const string Matrix = "matrix";
        public const string Label = "label";
        public const string Form = "form";
        public const string Simple = "simple";
        public const string SpaceDelimited = "spaceDelimited";
        public const string PipeDelimited = "pipeDelimited";
        public const string DeepObject = "deepObject";
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
        internal const string Example = "example";
        internal const string Examples = "examples";
    }

    private protected Parameter(JsonNodeReader reader)
    {
        _reader = reader;
        Description = ReadDescription();
        Content = ReadContent();
        Schema = ReadSchema();
        AssertSchemaOrContent();
        Deprecated = ReadDeprecated();
        Example = ReadExample();
        Examples = ReadExamples();
        AssertValidExamples();
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

    protected string? ReadStyle()
    {
        if (!_reader.TryRead(Keys.Style, out var styleReader))
            return null;

        Annotations.Add(styleReader);
        return styleReader.GetValue<string>();
    }
    protected void AssertStyle(params string[] validStyles)
    {
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
    protected bool ReadExplode()
    {
        if (!_reader.TryRead(Keys.Explode, out var explodeReader))
            return Style == Styles.Form;

        Annotations.Add(explodeReader);
        return explodeReader.GetValue<bool>();
    }
    private JsonNode? ReadExample()
    {
        if (!_reader.TryRead(Keys.Example, out var exampleReader))
            return null;

        var (_, value) = exampleReader;
        Annotations.Add(exampleReader);
        return value;
    }
    private Examples? ReadExamples()
    {
        if (!_reader.TryRead(Keys.Examples, out var examplesReader))
            return null;

        var examples = Examples.Parse(examplesReader);
        Annotations.Add(examplesReader.Key, new JsonObject(examples.Annotations));
        return examples;
    }
    private void AssertValidExamples()
    {
        if (Examples != null && Example != null)
            throw new InvalidOperationException($"'{Keys.Example}' and '{Keys.Examples}' are mutually exclusive");
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
    public abstract string Style { get; protected init; }
    public bool Deprecated { get; private init; }
    public abstract bool Explode { get; protected init; }
    public JsonNode? Example { get; private init; }
    public Examples? Examples { get; private init; }

    internal abstract class ParameterEvaluator
    {
        private readonly OpenApiEvaluationContext _openApiEvaluationContext;
        private readonly Parameter _parameter;
        private IParameterValueConverter? _converter;

        protected ParameterEvaluator(OpenApiEvaluationContext openApiEvaluationContext, Parameter parameter)
        {
            _openApiEvaluationContext = openApiEvaluationContext;
            _parameter = parameter;
        }

        private IParameterValueConverter GetParameterValueConverter(JsonSchema schema)
        {
            var converter = _openApiEvaluationContext.EvaluationOptions.ParameterValueConverters.FirstOrDefault(converter =>
                converter.ParameterLocation == _parameter.In &&
                converter.ParameterName == _parameter.Name);
            return converter ?? new SchemaParameterValueConverter(_parameter, schema);
        }

        protected void Evaluate(string[] values)
        {
            var schemaEvaluator = _parameter.Schema?.GetEvaluator(_openApiEvaluationContext);
            if (schemaEvaluator != null)
            {
                var schema = schemaEvaluator.ResolveSchema();
                _converter ??= GetParameterValueConverter(schema);
                if (!_converter.TryMap(values, out var instance, out var mappingError))
                {
                    _openApiEvaluationContext.Results.Fail(mappingError);
                    return;
                }

                schemaEvaluator.Evaluate(instance);
                return;
            }

            if (_parameter.Content != null &&
                _parameter.Content.GetEvaluator(_openApiEvaluationContext)
                    .TryMatch(MediaTypeValue.ApplicationJson, out var mediaTypeEvaluator))
            {
                if (values.Length != 1)
                {
                    _openApiEvaluationContext.Results.Fail($"Expected 1 value when the parameter is as json content, found {values.Length}");
                }
                mediaTypeEvaluator.Evaluate(values.First());
            }
        }
    }
}