using System.Collections.ObjectModel;
using System.Text.Json.Nodes;
using OpenAPI.Evaluation.Http;

namespace OpenAPI.Evaluation.Specification;

public partial class Server
{
    private readonly JsonNodeReader _reader;
    private readonly Dictionary<string, JsonNode?> _annotations = new();

    private Server(JsonNodeReader reader)
    {
        _reader = reader;

        var url = _reader.Read("url").GetValue<string>().Trim();
        Url = new Uri(url, UriKind.RelativeOrAbsolute);

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
    }
    
    internal static Server Parse(JsonNodeReader reader) => new(reader);

    public Uri Url { get; }
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
            if (_server.Url.IsAbsoluteUri)
            {
                if (!uri.IsAbsoluteUri)
                {
                    DoesNotMatch();
                    return false;
                }

                var serverParts = _server.Url.GetLeftPart(UriPartial.Path).TrimEnd('/');
                var uriParts = uri.GetLeftPart(UriPartial.Path).TrimEnd('/');
                if (serverParts == uriParts)
                    return true;

                DoesNotMatch();
                return false;
            }

            if (uri.IsAbsoluteUri)
            {
                DoesNotMatch();
                return false;
            }

            var serverPathSegments = _server.Url.GetPathSegments();
            var uriPathSegments = uri.GetPathSegments();
            // todo: match against server variables
            if (serverPathSegments.SequenceEqual(uriPathSegments))
                return true;

            DoesNotMatch();
            return false;

            void DoesNotMatch()
            {
                _openApiEvaluationContext.Results.Fail($"{uri} does not match server url {_server.Url}");
            }
        }
    }
}