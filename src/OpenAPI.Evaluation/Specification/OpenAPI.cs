using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using Json.Pointer;
using Json.Schema;
using OpenAPI.Evaluation.ParameterParsers;

namespace OpenAPI.Evaluation.Specification;

public sealed class OpenAPI
{
    private readonly SemVer _from = "3.1.0";
    // OAS does not follow semver semantics strictly, so we follow the latest minor version
    // https://github.com/OAI/OpenAPI-Specification/blob/2ea76e67ab1c4be2013ff6b7f6bda230901617ae/versions/3.1.0.md#versions
    private readonly SemVer _to = "3.2.0";

    private readonly OpenApiEvaluationOptions _evaluationOptions;
    private readonly JsonNodeReader _reader;

    private OpenAPI(
        JsonNode document,
        Uri? baseUri = null,
        IParameterValueParser[]? parameterValueParsers = null)
    {
        _reader = new JsonNodeReader(document, JsonPointer.Empty);
        OpenApi = _reader.Read("openapi").GetValue<string>();
        EnsureSupportedOpenApiVersion();
        Servers = Servers.Parse(_reader.Read("servers"));

        baseUri ??= new Uri(Servers[0].Url, UriKind.RelativeOrAbsolute);
        if (!baseUri.IsAbsoluteUri)
            throw new ArgumentException("The servers url property in the specification must have an absolute URI when base uri is not explicitly provided", nameof(baseUri));

        Paths = Paths.Parse(_reader.Read("paths"));

        var baseDocument = new JsonNodeBaseDocument(document, baseUri);

        var jsonSchemaEvaluationOptions = new Json.Schema.EvaluationOptions
        {
            OutputFormat = OutputFormat.Hierarchical,
            EvaluateAs = SpecVersion.Draft202012
        };
        Json.Schema.OpenApi.Vocabularies.Register(jsonSchemaEvaluationOptions.VocabularyRegistry, jsonSchemaEvaluationOptions.SchemaRegistry);
        jsonSchemaEvaluationOptions.SchemaRegistry.Register(baseDocument);
        _evaluationOptions = new OpenApiEvaluationOptions
        {
            JsonSchemaEvaluationOptions = jsonSchemaEvaluationOptions,
            Document = baseDocument
        };
        if (parameterValueParsers != null)
        {
            _evaluationOptions.ParameterValueParsers.AddRange(parameterValueParsers);
        }
    }

    private void EnsureSupportedOpenApiVersion()
    {
        SemVer version = OpenApi;
        if (version < _from ||
            version >= _to)
        {
            throw new InvalidOperationException($"OpenAPI version {OpenApi} is not supported. Supported versions are [{_from}, {_to})");
        }
    }

    /// <summary>
    /// Parses an OpenAPI 3.1 specification
    /// </summary>
    /// <param name="document">The OpenAPI specification</param>
    /// <param name="baseUri">The base url of the specification.
    /// If not specified the first url in the specification's server node will be used.
    /// Must be an absolute URL</param>
    /// <param name="parameterValueParsers">A list of parameter value parsers that can override the default logic of parsing parameter values to json objects</param>
    /// <returns>The parsed OpenAPI specification</returns>
    public static OpenAPI Parse(
        JsonNode document, 
        Uri? baseUri = null,
        IParameterValueParser[]? parameterValueParsers = null) => new(document, baseUri, parameterValueParsers);

    public Uri BaseUri => _evaluationOptions.Document.BaseUri;
    public string OpenApi { get; }
    public Servers Servers { get; }
    public Paths Paths { get; }

    /// <summary>
    /// Try to match an OpenAPI operation
    /// </summary>
    /// <param name="uri">Uri to match</param>
    /// <param name="method">Method to match</param>
    /// <param name="operationEvaluator">Evaluator to the operation that matched</param>
    /// <param name="evaluationResults">Evaluation results</param>
    /// <returns>true if operation was matched</returns>
    /// <exception cref="ArgumentException">Uri must be absolute</exception>
    public bool TryMatchApiOperation(Uri uri, string method, [NotNullWhen(true)] out Operation.Evaluator? operationEvaluator,
        out OpenApiEvaluationResults evaluationResults)
    {
        if (!uri.IsAbsoluteUri)
        {
            throw new ArgumentException("Uri is not an absolute uri", nameof(uri));
        }

        var rootEvaluationContext = new OpenApiEvaluationContext(_reader, _evaluationOptions);
        evaluationResults = rootEvaluationContext.Results;
        operationEvaluator = null;

        if (!Paths.GetEvaluator(rootEvaluationContext).TryMatch(uri,
                out var pathItemEvaluator,
                out var serverUri))
            return false;

        if (!pathItemEvaluator.TryMatch(method, out operationEvaluator))
            return false;

        if (!operationEvaluator.TryGetServers(out var serversEvaluator))
        {
            if (!pathItemEvaluator.TryGetServers(out serversEvaluator))
            {
                serversEvaluator = Servers.GetEvaluator(rootEvaluationContext);
            }
        }
        return serversEvaluator.TryMatch(serverUri);
    }

    public override string ToString() =>
        _evaluationOptions.Document.BaseUri.ToString();
}
