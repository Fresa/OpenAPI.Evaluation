using System.Diagnostics.CodeAnalysis;
using UriExtensions = OpenAPI.Validation.Http.UriExtensions;

namespace OpenAPI.Validation.Specification;

public partial class Server
{
    private readonly JsonNodeReader _reader;

    private Server(JsonNodeReader reader)
    {
        _reader = reader;

        var url = _reader.Read("url").GetValue<string>().Trim();
        if (!url.EndsWith('/'))
            url += "/";
        Url = new Uri(url);

        if (_reader.TryRead("description", out var descriptionReader))
        {
            Description = descriptionReader.GetValue<string>();
        }
    }
    
    internal static Server Parse(JsonNodeReader reader) => new(reader);

    public Uri Url { get; }
    public string? Description { get; }

    internal Evaluator GetEvaluator(OpenApiEvaluationContext openApiEvaluationContext)
    {
        return new Evaluator(openApiEvaluationContext.Evaluate(_reader), this);
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

        internal bool TryMatch(Uri uri, [NotNullWhen(true)] out Uri? relativeUri)
        {
            relativeUri = null;
            
            if (_server.Url.IsAbsoluteUri)
            {
                if (!uri.IsAbsoluteUri)
                {
                    DoesNotMatch();
                    return false;
                }

                relativeUri = _server.Url.MakeRelativeUri(uri);
                if (relativeUri != uri) 
                    return true;
                
                relativeUri = null;
                DoesNotMatch();
                return false;
            }

            if (uri.IsAbsoluteUri)
            {
                DoesNotMatch();
                return false;
            }

            var serverPathSegments = UriExtensions.GetPathSegments(_server.Url);
            var uriPathSegments = UriExtensions.GetPathSegments(uri);
            if (serverPathSegments.Length > uriPathSegments.Length)
            {
                DoesNotMatch();
                return false;
            }

            // todo: match against server variables
            if (serverPathSegments.SequenceEqual(uriPathSegments.Take(serverPathSegments.Length)))
            {
                var relativeSegments = uriPathSegments.Skip(serverPathSegments.Length);
                relativeUri = new Uri("/" + string.Join('/', relativeSegments), UriKind.Relative);
                return true;
            }

            DoesNotMatch();
            return false;

            void DoesNotMatch()
            {
                _openApiEvaluationContext.Results.Fail($"{uri} does not match url {_server.Url}");
            }
        }
    }
}