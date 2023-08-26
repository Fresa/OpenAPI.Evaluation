using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using Json.Pointer;
using Json.Schema;
using OpenAPI.Validation.Specification;

namespace OpenAPI.Validation;

public sealed class OpenApiDocument
{
    private readonly SemVer _from = "3.1.0";
    // OAS does not follow semver semantics strictly, so we follow the latest minor version
    // https://github.com/OAI/OpenAPI-Specification/blob/2ea76e67ab1c4be2013ff6b7f6bda230901617ae/versions/3.1.0.md#versions
    private readonly SemVer _to = "3.2.0";

    private readonly JsonNodeBaseDocument _baseDocument;
    private readonly EvaluationOptions _evaluationOptions = new()
    {
        OutputFormat = OutputFormat.Hierarchical,
        EvaluateAs = SpecVersion.Draft202012
    };
    private readonly string _basePath;
    private readonly JsonNodeReader _document;

    private OpenApiDocument(JsonNode document, Uri? baseUri = null)
    {
        if (baseUri != null && !baseUri.IsAbsoluteUri)
            throw new ArgumentException("Base uri must be an absolute URI", nameof(baseUri));

        _document = new JsonNodeReader(document, JsonPointer.Empty);
        EnsureSupportedOpenApiVersion(_document);
        var serverUri = GetServerUri(_document);

        baseUri ??= serverUri;
        if (!baseUri.IsAbsoluteUri)
            throw new ArgumentException("The servers url property in the specification must have an absolute URI when base uri is not explicitly provided", nameof(baseUri));

        _basePath = serverUri.AbsolutePath.Trim('/');
        _baseDocument = new JsonNodeBaseDocument(document, baseUri);
        Json.Schema.OpenApi.Vocabularies.Register(_evaluationOptions.VocabularyRegistry, _evaluationOptions.SchemaRegistry);
        _evaluationOptions.SchemaRegistry.Register(_baseDocument);

        Servers = Servers.Parse(_document.Read("servers"));
        Paths = Paths.Parse(_document.Read("paths"));
    }
    
    private static Uri GetServerUri(JsonNodeReader reader)
    {
        var serverUrlPointer = JsonPointer.Parse("#/servers/0/url");
        var serverUrl = reader.Read(serverUrlPointer);
        return new Uri(serverUrl.GetValue<string>(), UriKind.RelativeOrAbsolute);
    }

    private void EnsureSupportedOpenApiVersion(JsonNodeReader reader)
    {
        var versionString = reader.Read("openapi").GetValue<string>();
        SemVer version = versionString;
        if (version < _from ||
            version >= _to)
            throw new InvalidOperationException($"OpenAPI version {versionString} is not supported. Supported versions are [{_from}, {_to})");
    }

    public static OpenApiDocument Parse(JsonNode document, Uri? baseUri = null) => new(document, baseUri);

    public Servers Servers { get; }

    public Paths Paths { get; }

    public bool TryGetApiOperation(HttpRequestMessage message, [NotNullWhen(true)] out Operation.Evaluator? operation,
        out OpenApiEvaluationResults evaluationResults)
    {
        var rootEvaluationContext = new OpenApiEvaluationContext(_baseDocument, _document, _evaluationOptions);
        evaluationResults = rootEvaluationContext.Results;
        operation = null;

        var requestUri = message.RequestUri ??
                         throw new ArgumentNullException($"{nameof(message)}.{nameof(message.RequestUri)}");
        if (!requestUri.IsAbsoluteUri)
        {
            throw new ArgumentNullException($"{nameof(message)}.{nameof(message.RequestUri)}", "Request URI is not an absolute uri");
        }

        if (!Servers.GetEvaluator(rootEvaluationContext).TryMatch(requestUri, out var relativeUri))
            return false;

        if (!Paths.GetEvaluator(rootEvaluationContext).TryMatch(relativeUri, out var pathItemEvaluator))
            return false;

        if (!pathItemEvaluator.TryMatch(message.Method.Method, out var foundOperation))
            return false;

        operation = foundOperation;
        return true;
    }
    
    public override string ToString() => 
        _baseDocument.BaseUri.ToString();

    private readonly struct SemVer
    {
        private int Major { get; init; }
        private int Minor { get; init; }
        private int Patch { get; init; }

        public static implicit operator SemVer(string semverString)
        {
            var parts = semverString.Split('.');
            if (parts.Length != 3)
                throw new ArgumentException($"{semverString} does not consist of three parts", nameof(semverString));
            if (!int.TryParse(parts[0], out var major))
                throw new ArgumentException($"The major part of {semverString} is not a valid integer", nameof(semverString));
            if (!int.TryParse(parts[1], out var minor))
                throw new ArgumentException($"The minor part of {semverString} is not a valid integer", nameof(semverString));
            if (!int.TryParse(parts[2], out var patch))
                throw new ArgumentException($"The patch part of {semverString} is not a valid integer", nameof(semverString));
            return new SemVer
            {
                Major = major,
                Minor = minor,
                Patch = patch
            };
        }

        public override string ToString() =>
            $"{Major}.{Minor}.{Patch}";

        public static bool operator ==(SemVer current, SemVer other) =>
            current.Equals(other);
        public static bool operator !=(SemVer current, SemVer other) =>
            !current.Equals(other);
        public static bool operator >(SemVer current, SemVer other)
        {
            if (current.Major > other.Major)
                return true;
            if (current.Major < other.Major)
                return false;
            if (current.Minor > other.Minor)
                return true;
            if (current.Minor < other.Minor)
                return false;
            return current.Patch > other.Patch;
        }
        public static bool operator >=(SemVer current, SemVer other) =>
            current > other ||
            current == other;
        public static bool operator <(SemVer current, SemVer other) =>
            !(current >= other);
        public static bool operator <=(SemVer current, SemVer other) =>
            current < other ||
            current == other;
        public override int GetHashCode() =>
            HashCode.Combine(Major, Minor, Patch);
        public override bool Equals(object? obj) =>
            obj is SemVer other &&
            GetHashCode() == other.GetHashCode();
    }
}
