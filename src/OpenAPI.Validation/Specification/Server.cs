namespace OpenAPI.Validation.Specification;

public partial class Server
{
    private readonly JsonNodeReader _reader;

    internal Server(JsonNodeReader reader)
    {
        _reader = reader;
    }

    private UrlObject? _url;

    public UrlObject Url
    {
        get
        {
            return _url ??= new UrlObject(_reader.Read("url"));
        }
    }

    internal Evaluator GetEvaluator(OpenApiEvaluationContext openApiEvaluationContext)
    {
        return new Evaluator(openApiEvaluationContext.Evaluate(_reader), this);
    }

    internal class Evaluator
    {
        private readonly OpenApiEvaluationContext _openApiEvaluationContext;
        private readonly Server _server;

        internal Evaluator(OpenApiEvaluationContext openApiEvaluationContext, Server server)
        {
            _openApiEvaluationContext = openApiEvaluationContext;
            _server = server;
        }

        internal OpenApiEvaluationResults Evaluate(Uri uri)
        {
            var evaluator = _server.Url.GetEvaluator(_openApiEvaluationContext);
            var results = evaluator.Evaluate(uri);

            return _openApiEvaluationContext.Results;
        }
    }

    public partial class UrlObject
    {
        private readonly JsonNodeReader _reader;

        internal UrlObject(JsonNodeReader reader)
        {
            _reader = reader;
        }

        private Uri? _value;
        public Uri Value => _value ??= new Uri(_reader.GetValue<string>(), UriKind.RelativeOrAbsolute);

        internal Evaluator GetEvaluator(OpenApiEvaluationContext openApiEvaluationContext)
        {
            return new Evaluator(openApiEvaluationContext.Evaluate(_reader), this);
        }

        internal class Evaluator
        {
            private readonly OpenApiEvaluationContext _openApiEvaluationContext;
            private readonly Uri _value;

            internal Evaluator(OpenApiEvaluationContext openApiEvaluationContext, UrlObject urlObject)
            {
                _openApiEvaluationContext = openApiEvaluationContext;
                _value = urlObject.Value;
            }

            internal OpenApiEvaluationResults Evaluate(Uri uri)
            {
                var results = _openApiEvaluationContext.Results;

                if (uri == _value)
                    return results;

                if (!_value.IsAbsoluteUri)
                {
                    if (uri.AbsolutePath.StartsWith(_value.AbsolutePath))
                        return results;
                    DoesNotMatch();
                    return results;
                }

                // todo: Match against server variables
                var relativeUri = _value.MakeRelativeUri(uri);
                if (relativeUri == uri)
                {
                    DoesNotMatch();
                    return results;
                }

                return results;

                void DoesNotMatch()
                {
                    _openApiEvaluationContext.Results.Fail($"{uri} does not match url {_value}");
                }
            }

        }
    }
}