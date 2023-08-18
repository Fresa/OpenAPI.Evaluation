namespace OpenAPI.Validation.Specification;

public sealed partial class PathItem
{
    private readonly JsonNodeReader _reader;

    private PathItem(JsonNodeReader reader)
    {
        _reader = reader;

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
            _operations.Add(operation);
            return operation;
        }
    }
    
    internal static PathItem Parse(JsonNodeReader reader)
    {
        return new PathItem(reader);
    }
    
    public Operation? Get { get; private init; }
    public Operation? Put { get; internal init; }
    public Operation? Post { get; private init; }
    public Operation? Delete { get; private init; }
    public Operation? Options { get; private init; }
    public Operation? Head { get; private init; }
    public Operation? Patch { get; private init; }
    public Operation? Trace { get; private init; }

    private List<Operation> _operations = new();
    public IReadOnlyList<Operation> Operations => _operations.AsReadOnly();

    internal Evaluator GetEvaluator(OpenApiEvaluationContext openApiEvaluationContext)
    {
        return new Evaluator(openApiEvaluationContext.Evaluate(_reader), this);
    }

    internal class Evaluator
    {
        private readonly OpenApiEvaluationContext _openApiEvaluationContext;
        private readonly PathItem _pathItem;

        internal Evaluator(OpenApiEvaluationContext openApiEvaluationContext, PathItem pathItem)
        {
            _openApiEvaluationContext = openApiEvaluationContext;
            _pathItem = pathItem;
        }

        internal OpenApiEvaluationResults Match(string method)
        {
            _openApiEvaluationContext.Results.OneDetail();
            foreach (var pathItem in _pathItem.Operations)
            {
                pathItem.GetEvaluator(_openApiEvaluationContext).Evaluate(uri);
            }

            return _openApiEvaluationContext.Results;
        }
    }

    internal IEnumerable<OpenApiOperation> Evaluate(string method)
    {
        foreach (var methodEvaluationContext in _evaluationContext.EvaluateChildren())
        {
            var evaluatedMethod = methodEvaluationContext.GetKey();
            if (evaluatedMethod.Equals(method, StringComparison.CurrentCultureIgnoreCase))
            {
                yield return new OpenApiOperation(_routePattern, methodEvaluationContext);
                continue;
            }

            _evaluationContext.Results.Fail($"'{method}' does not match '{evaluatedMethod}'");
        }
    }
}