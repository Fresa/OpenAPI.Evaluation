using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;

namespace OpenAPI.Validation.Specification;

public partial class Servers
{
    private readonly JsonNodeReader _reader;

    private Servers(JsonNodeReader reader)
    {
        _reader = reader;

        foreach (var serverReader in _reader.ReadChildren())
        {
            _servers.Add(Server.Parse(serverReader));
        }

        if (!_servers.Any())
        {
            var defaultServer = JsonNode.Parse("""
            {
                "url": "/"
            }
            """) ?? throw new InvalidOperationException("Internal parsing error");
            _servers.Add(Server.Parse(new JsonNodeReader(defaultServer, reader.Trail)));
        }
    }

    internal static Servers Parse(JsonNodeReader reader) => new(reader);

    private readonly List<Server> _servers = new();
    public IReadOnlyList<Server> ServerObjects => _servers.AsReadOnly();

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
            _openApiEvaluationContext.Results.OneDetail();

            _servers = servers;
        }

        internal bool TryMatch(Uri uri, [NotNullWhen(true)] out Uri? relativeUri)
        {
            relativeUri = null;
            foreach (var serverObject in _servers.ServerObjects)
            {
                serverObject.GetEvaluator(_openApiEvaluationContext).TryMatch(uri, out relativeUri);
            }
            return _openApiEvaluationContext.Results.IsValid;
        }
    }
}