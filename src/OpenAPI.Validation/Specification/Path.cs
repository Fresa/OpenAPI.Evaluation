using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using OpenAPI.Evaluation.Collections;

namespace OpenAPI.Evaluation.Specification;

public sealed partial class Path
{
    private readonly JsonNodeReader _reader;
    private readonly IDictionary<string, JsonNode?> _annotations = new Dictionary<string, JsonNode?>();

    private Path(JsonNodeReader reader)
    {
        _reader = reader;

        if (_reader.TryRead("summary", out var summaryReader))
        {
            Summary = summaryReader.GetValue<string>();
            _annotations.Add(summaryReader);
        }
        if (_reader.TryRead("description", out var descriptionReader))
        {
            Description = descriptionReader.GetValue<string>();
            _annotations.Add(descriptionReader);
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

        var duplicatedOperationIds = Operations.Values
            .Where(operation => operation.OperationId != null)
            .GroupBy(operation => operation.OperationId)
            .Where(grouping => grouping.Count() > 1)
            .ToList();
        if (duplicatedOperationIds.Any())
        {
            throw new ArgumentException(
                $"Operations cannot share operation id: {string.Join(", ", duplicatedOperationIds.Select(grouping => grouping.Key))}");
        }

        Operation? ReadOperation(string name)
        {
            if (!reader.TryRead(name, out var operationReader))
                return null;

            var operation = Operation.Parse(operationReader);
            _operations.Add(name, operation);
            return operation;
        }
    }

    internal static Path Parse(JsonNodeReader reader) => new(reader);
    public string? Summary { get; }
    public string? Description { get; }
    public Operation? Get { get; }
    public Operation? Put { get; }
    public Operation? Post { get; }
    public Operation? Delete { get; }
    public Operation? Options { get; }
    public Operation? Head { get; }
    public Operation? Patch { get; }
    public Operation? Trace { get; }

    private readonly Dictionary<string, Operation> _operations = new();
    public IReadOnlyDictionary<string, Operation> Operations => _operations.AsReadOnly();
    public Parameters? Parameters { get; }
    public Servers? Servers { get; }

    internal Evaluator GetEvaluator(OpenApiEvaluationContext openApiEvaluationContext, RoutePattern routePattern)
    {
        var context = openApiEvaluationContext.Evaluate(_reader);
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

                Parameters? nonOverriddenPathParameters = null;
                if (_pathItem.Parameters != null)
                {
                    nonOverriddenPathParameters =
                        operationObject.Parameters == null
                            ? _pathItem.Parameters
                            : _pathItem.Parameters.Except(operationObject.Parameters);
                }

                operationEvaluator = operationObject.GetEvaluator(
                    _openApiEvaluationContext,
                    _routePattern,
                    nonOverriddenPathParameters?.GetEvaluator(_openApiEvaluationContext));
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