using System.Text.Json.Nodes;
using OpenAPI.Evaluation.Http;

namespace OpenAPI.Evaluation.Specification;

public partial class Server
{
    private readonly JsonNodeReader _reader;
    private readonly Dictionary<string, JsonNode?> _annotations = new();
    private readonly List<(string Segment, bool IsVariableKey)> _urlSegments = new();

    private Server(JsonNodeReader reader)
    {
        _reader = reader;

        Url = _reader.Read("url").GetValue<string>().Trim('/');
        
        if (_reader.TryRead("description", out var descriptionReader))
        {
            Description = descriptionReader.GetValue<string>();
            var (name, value) = descriptionReader.GetProperty();
            _annotations.Add(name, value);
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
    public IReadOnlyList<(string Segment, bool IsVariableKey)> UrlTemplateSegments => _urlSegments.AsReadOnly();
    public string? Description { get; }
    public ServerVariables? Variables { get; }

    internal Evaluator GetEvaluator(OpenApiEvaluationContext openApiEvaluationContext)
    {
        var context = openApiEvaluationContext.Evaluate(_reader);
        if (_annotations.Any())
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

        private static bool MatchValue(string uri, string variableValue, ref int position)
        {
            var length = variableValue.Length;
            if (uri.Length < position + length)
            {
                return false;
            }

            if (!uri[position..(position + length)].Equals(variableValue,
                    StringComparison.CurrentCultureIgnoreCase))
            {
                return false;
            }

            position += length;
            return true;
        }

        private bool MatchUri(Uri uri)
        {
            var uriString = uri.GetLeftPart(UriPartial.Path);
            if (!_server.Url.Contains("://"))
            {
                uriString = uri.GetPath();
            }
            uriString = uriString.Trim('/');

            var position = 0;
            foreach (var (segment, isVariableKey) in _server.UrlTemplateSegments)
            {
                if (isVariableKey)
                {
                    var serverVariable = _server.Variables![segment];
                    if (serverVariable.Enum == null)
                    {
                        if (!MatchValue(uriString, serverVariable.Default, ref position))
                        {
                            return false;
                        }
                        continue;
                    }

                    if (!serverVariable.Enum.Any(@enum => MatchValue(uriString, @enum, ref position)))
                    {
                        return false;
                    }
                    continue;
                }

                if (!MatchValue(uriString, segment, ref position))
                {
                    return false;
                }
            }

            return position == uriString.Length;
        }
    }
}
