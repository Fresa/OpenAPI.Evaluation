using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;

namespace OpenAPI.Validation.Specification;

public sealed partial class Path
{
    private readonly JsonNodeReader _reader;
    private readonly Dictionary<string, JsonNode?> _annotations = new();

    private Path(JsonNodeReader reader)
    {
        _reader = reader;

        if (_reader.TryRead("summary", out var summaryReader))
        {
            Summary = summaryReader.GetValue<string>();
            var (name, value) = summaryReader.GetProperty();
            _annotations.Add(name, value);
        }
        if (_reader.TryRead("description", out var descriptionReader))
        {
            Description = descriptionReader.GetValue<string>();
            var (name, value) = descriptionReader.GetProperty();
            _annotations.Add(name, value);
        }

        if (_reader.TryRead("parameters", out var parametersReader))
        {
            Parameters = Parameters.Parse(parametersReader);
        }
        if (_reader.TryRead("servers", out var serversReader))
        {
            Servers = Servers.Parse(serversReader);
        }

        Get = ReadOperation("get");
        Put = ReadOperation("put");
        Post = ReadOperation("post");
        Delete = ReadOperation("delete");
        Options = ReadOperation("options");
        Head = ReadOperation("head");
        Patch = ReadOperation("patch");
        Trace = ReadOperation("trace");

        Operation? ReadOperation(string name)
        {
            if (!reader.TryRead(name, out var operationReader)) 
                return null;

            var operation = Operation.Parse(operationReader, Parameters);
            _operations.Add(name, operation);
            return operation;
        }
    }
    
    internal static Path Parse(JsonNodeReader reader) => new Path(reader);
    public string? Summary { get; }
    public string? Description { get; set; }
    public Operation? Get { get; private init; }
    public Operation? Put { get; internal init; }
    public Operation? Post { get; private init; }
    public Operation? Delete { get; private init; }
    public Operation? Options { get; private init; }
    public Operation? Head { get; private init; }
    public Operation? Patch { get; private init; }
    public Operation? Trace { get; private init; }

    private readonly Dictionary<string, Operation> _operations = new();
    public IReadOnlyDictionary<string, Operation> Operations => _operations.AsReadOnly();
    public Parameters? Parameters { get; }
    public Servers? Servers { get; }

    internal Evaluator GetEvaluator(OpenApiEvaluationContext openApiEvaluationContext, RoutePattern routePattern)
    {
        var context = openApiEvaluationContext.Evaluate(_reader);
        if (_annotations.Any())
            context.Results.SetAnnotations(_annotations);
        return new Evaluator(context, this, routePattern);
    }

    internal sealed class Evaluator
    {
        private readonly OpenApiEvaluationContext _openApiEvaluationContext;
        private readonly Path _pathItem;
        private readonly RoutePattern _routePattern;

        internal Evaluator(OpenApiEvaluationContext openApiEvaluationContext, Path pathItem, RoutePattern routePattern)
        {
            _openApiEvaluationContext = openApiEvaluationContext;
            _pathItem = pathItem;
            _routePattern = routePattern;
        }
        
        internal bool TryMatch(string method, [NotNullWhen(true)] out Operation.Evaluator? operationEvaluator)
        {
            foreach (var (operationMethod, operationObject) in _pathItem.Operations)
            {
                if (!operationMethod.Equals(method, StringComparison.CurrentCultureIgnoreCase)) 
                    continue;

                operationEvaluator = operationObject.GetEvaluator(_openApiEvaluationContext, _routePattern);
                return true;
            }

            _openApiEvaluationContext.Results.Fail($"'{method}' does not match any of the operations '{string.Join(", ", _pathItem.Operations.Keys)}'");
            operationEvaluator = null;
            return false;
        }

        public bool TryGetServers([NotNullWhen(true)] out Servers.Evaluator? serversEvaluator)
        {
            if (_pathItem.Servers == null)
            {
                serversEvaluator = null;
                return false;
            }

            serversEvaluator = _pathItem.Servers.GetEvaluator(_openApiEvaluationContext);
            return true;
        }
    }
}