using System.Collections;
using System.Text.Json.Nodes;

namespace OpenAPI.Evaluation.Specification;

public sealed partial class Servers : IReadOnlyList<Server>
{
    private readonly JsonNodeReader _reader;
    private readonly List<Server> _servers = new();

    private Servers(JsonNodeReader reader)
    {
        _reader = reader;

        foreach (var serverReader in _reader.ReadChildren())
        {
            _servers.Add(Server.Parse(serverReader));
        }

        if (_servers.Any())
            return;

        var defaultServer = JsonNode.Parse("""
            {
                "url": "/"
            }
            """) ?? throw new InvalidOperationException("Internal parsing error");
        _servers.Add(Server.Parse(new JsonNodeReader(defaultServer, reader.Trail)));
    }

    internal static Servers Parse(JsonNodeReader reader) => new(reader);


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
            _openApiEvaluationContext.Results.IsValidWhenExactlyOneDetailIsValid();

            _servers = servers;
        }

        internal bool TryMatch(Uri uri) =>
            _servers
                .Any(serverObject => serverObject
                    .GetEvaluator(_openApiEvaluationContext)
                    .TryMatch(uri));
    }

    public IEnumerator<Server> GetEnumerator() => _servers.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public int Count => _servers.Count;
    public Server this[int index] => _servers[index];
}