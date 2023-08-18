namespace OpenAPI.Validation.Specification;

public partial class Servers
{
    private readonly JsonNodeReader _reader;

    internal Servers(JsonNodeReader reader)
    {
        _reader = reader;
    }

    public IEnumerable<Server> ServerObjects => _reader.ReadChildren()
        .Select(reader => new Server(reader));

    internal Evaluator GetEvaluator(OpenApiEvaluationContext openApiEvaluationContext)
    {
        return new Evaluator(openApiEvaluationContext.Evaluate(_reader), this);
    }

    internal class Evaluator
    {
        private readonly OpenApiEvaluationContext _openApiEvaluationContext;
        private readonly Servers _servers;

        internal Evaluator(OpenApiEvaluationContext openApiEvaluationContext, Servers servers)
        {
            _openApiEvaluationContext = openApiEvaluationContext;
            _servers = servers;
        }

        internal bool TryMatch(Uri uri)
        {
            _openApiEvaluationContext.Results.AnyDetails();
            foreach (var serverObject in _servers.ServerObjects)
            {
                serverObject.GetEvaluator(_openApiEvaluationContext).Evaluate(uri);
            }

            return _openApiEvaluationContext.Results;
        }
    }

}