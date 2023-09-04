using System.Text.Json.Nodes;
using OpenAPI.Evaluation.Collections;
using OpenAPI.Evaluation.Http;

namespace OpenAPI.Evaluation.Specification;

public partial class Server
{
    private readonly JsonNodeReader _reader;
    private readonly IDictionary<string, JsonNode?> _annotations = new Dictionary<string, JsonNode?>();
    private readonly List<(string Segment, bool IsVariableKey)> _urlSegments = new();

    private Server(JsonNodeReader reader)
    {
        _reader = reader;

        var urlReader = _reader.Read("url");
        Url = urlReader.GetValue<string>().Trim('/');
        _annotations.Add(urlReader);

        if (_reader.TryRead("description", out var descriptionReader))
        {
            Description = descriptionReader.GetValue<string>();
            _annotations.Add(descriptionReader);
        }

        if (_reader.TryRead("variables", out var variablesReader))
        {
            Variables = ServerVariables.Parse(variablesReader);
        }

        var i = 0;
        while (i < Url.Length)
        {
            var curlyStartIdx = Url.IndexOf('{', i);
            if (curlyStartIdx == -1)
            {
                var invalidCurlyEndIdx = Url.IndexOf('}', i);
                if (invalidCurlyEndIdx != -1)
                    throw new ArgumentException(
                        $$"""Server url {{Url}} is missing a "{" for the corresponding "}" at position {{invalidCurlyEndIdx}}")""");
                _urlSegments.Add((Url[i..], false));
                break;
            }

            _urlSegments.Add((Url[i..curlyStartIdx], false));

            var curlyEndIdx = Url.IndexOf('}', curlyStartIdx);
            if (curlyEndIdx == -1)
                throw new ArgumentException(
                    $$"""Server url {{Url}} is missing a "}" for the corresponding "{" at position {{curlyStartIdx}}")""");

            var variableKey = Url[(curlyStartIdx + 1)..curlyEndIdx];
            if (!Variables?.ContainsKey(variableKey) ?? false)
                throw new ArgumentException(
                    $"Server url {Url} contains variable with key {variableKey} that is not defined in the variables list");
            _urlSegments.Add((variableKey, true));
            i = curlyEndIdx + 1;
        }
    }

    internal static Server Parse(JsonNodeReader reader) => new(reader);

    public string Url { get; }
    private IReadOnlyList<(string Segment, bool IsVariableKey)> UrlTemplateSegments => _urlSegments.AsReadOnly();
    public string? Description { get; }
    public ServerVariables? Variables { get; }

    internal Evaluator GetEvaluator(OpenApiEvaluationContext openApiEvaluationContext)
    {
        var context = openApiEvaluationContext.Evaluate(_reader);
        context.Results.SetAnnotations(_annotations);
        return new Evaluator(context, this);
    }

    internal sealed class Evaluator
    {
        private readonly OpenApiEvaluationContext _openApiEvaluationContext;
        private readonly Server _server;

        internal Evaluator(OpenApiEvaluationContext openApiEvaluationContext, Server server)
        {
            _openApiEvaluationContext = openApiEvaluationContext;
            _server = server;
        }

        internal bool TryMatch(Uri uri)
        {
            if (!uri.IsAbsoluteUri ||
                !uri.Scheme.StartsWith("http"))
            {
                throw new ArgumentException("Uri must be an absolute http url");
            }

            if (MatchUri(uri))
            {
                return true;
            }

            _openApiEvaluationContext.Results.Fail($"{uri} does not match server url {_server.Url}");
            return false;
        }
        
        private bool MatchUri(Uri uri)
        {
            var uriString = (_server.Url.Contains("://")
                ? uri.GetLeftPart(UriPartial.Path)
                : uri.GetPath())
                .Trim('/');

            var position = 0;
            foreach (var (segment, isVariableKey) in _server.UrlTemplateSegments)
            {
                if (isVariableKey)
                {
                    if (MatchServerVariable(uriString, segment, ref position))
                    {
                        continue;
                    }
                    return false;
                }

                if (MatchValue(uriString, segment, ref position))
                {
                    continue;
                }
                return false;
            }

            return position == uriString.Length;
        }

        private bool MatchServerVariable(string uri, string variableName, ref int position)
        {
            var serverVariable = _server.Variables![variableName];
            if (serverVariable.Enum == null)
            {
                return MatchValue(uri, serverVariable.Default, ref position);
            }

            foreach (var @enum in serverVariable.Enum)
            {
                if (MatchValue(uri, @enum, ref position))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool MatchValue(string uri, string value, ref int position)
        {
            var length = value.Length;
            if (uri.Length < position + length)
            {
                return false;
            }

            if (!uri[position..(position + length)].Equals(value,
                    StringComparison.CurrentCultureIgnoreCase))
            {
                return false;
            }

            position += length;
            return true;
        }
    }
}
