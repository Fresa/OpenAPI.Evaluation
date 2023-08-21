using System.Diagnostics.CodeAnalysis;

namespace OpenAPI.Validation.Specification;

public sealed partial class Path
{
    internal JsonNodeReader Reader { get; }

    private Path(JsonNodeReader reader)
    {
        Reader = reader;

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

            var operation = Operation.Parse(operationReader);
            _operations.Add(name, operation);
            return operation;
        }
    }
    
    internal static Path Parse(JsonNodeReader reader)
    {
        return new Path(reader);
    }
    
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

    internal Evaluator GetEvaluator(OpenApiEvaluationContext openApiEvaluationContext, RoutePattern routePattern)
    {
        return new Evaluator(openApiEvaluationContext.Evaluate(Reader), this, routePattern);
    }

    internal sealed class Evaluator
    {
        private readonly OpenApiEvaluationContext _openApiEvaluationContext;
        private readonly Path _pathItem;
        private readonly RoutePattern _routePattern;

        internal Evaluator(OpenApiEvaluationContext openApiEvaluationContext, Path pathItem, RoutePattern routePattern)
        {
            _openApiEvaluationContext = openApiEvaluationContext;
            _openApiEvaluationContext.Results.OneDetail();

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
    }
}