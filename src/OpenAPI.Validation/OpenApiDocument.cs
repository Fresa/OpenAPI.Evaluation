using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using Json.Pointer;
using Json.Schema;

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

    public OpenApiDocument(JsonNode document, Uri? baseUri = null)
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

    public bool TryGetApiOperation(HttpRequestMessage message, [NotNullWhen(true)] out OpenApiOperation? operation, out OpenApiEvaluationResults evaluationResults)
    {
        var rootEvaluationContext = new OpenApiEvaluationContext(_baseDocument, _document, _evaluationOptions);
        evaluationResults = rootEvaluationContext.Results;
        var pathsEvaluationContext = rootEvaluationContext.Evaluate("paths");
        operation = null;

        var requestUri = message.RequestUri;
        if (requestUri == null)
        {
            rootEvaluationContext.Results.Fail("Request URI is missing");
            return false;
        }

        var path = requestUri.AbsolutePath.Trim('/');
        if (!path.StartsWith(_basePath))
        {
            rootEvaluationContext.Results.Fail($"Path '{path}' does not start with base path '{_basePath}'");
            return false;
        }

        var relativePath = path[_basePath.Length..].Trim('/');
        var requestedPathSegments = relativePath.Split('/');
        var method = message.Method.Method.ToLowerInvariant();

        pathsEvaluationContext.Results.AnyDetails();
        OpenApiEvaluationContext? foundPathEvaluationContext = null;
        RoutePattern? routePattern = null;
        foreach (var pathEvaluationContext in pathsEvaluationContext.EvaluateChildren())
        {
            var pathTemplate = pathEvaluationContext.GetKey();
            var apiPathSegments = pathTemplate.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (apiPathSegments.Length != requestedPathSegments.Length)
            {
                pathEvaluationContext.Results.Fail(DoesNotMatchPathErrorMessage());
                continue;
            }

            var match = true;
            var routeValues = new Dictionary<string, string>();
            for (var i = 0; i < apiPathSegments.Length; i++)
            {
                var segment = apiPathSegments[i];
                var requestedSegment = requestedPathSegments[i];
                if (segment.StartsWith('{') && segment.EndsWith('}'))
                {
                    routeValues.Add(segment.TrimStart('{').TrimEnd('}'), requestedSegment);
                    continue;
                }

                if (segment.Equals(requestedSegment, StringComparison.InvariantCultureIgnoreCase))
                    continue;

                match = false;
                break;
            }

            if (!match)
            {
                pathEvaluationContext.Results.Fail(DoesNotMatchPathErrorMessage());
                continue;
            }

            foundPathEvaluationContext = pathEvaluationContext;
            routePattern = new RoutePattern(pathTemplate, routeValues);

            string DoesNotMatchPathErrorMessage() => $"'{relativePath}' does not match '{pathTemplate}'";
        }

        if (foundPathEvaluationContext == null)
        {
            return false;
        }

        OpenApiEvaluationContext? foundMethodEvaluationContext = null;
        foreach (var methodEvaluationContext in foundPathEvaluationContext.EvaluateChildren())
        {
            var evaluatedMethod = methodEvaluationContext.GetKey();
            if (evaluatedMethod == method)
            {
                foundMethodEvaluationContext ??= methodEvaluationContext;
                continue;
            }
            methodEvaluationContext.Results.Fail($"'{method}' does not match '{evaluatedMethod}'");
        }

        if (foundMethodEvaluationContext == null)
            return false;

        operation = new OpenApiOperation(
            routePattern!,
            foundMethodEvaluationContext);
        return true;


        return false;
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