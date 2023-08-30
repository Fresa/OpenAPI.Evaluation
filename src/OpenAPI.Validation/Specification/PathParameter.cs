using System.Text.Json.Nodes;

namespace OpenAPI.Evaluation.Specification;

public sealed class PathParameter : Parameter
{
    private readonly JsonNodeReader _reader;

    private PathParameter(JsonNodeReader reader) : base(reader)
    {
        _reader = reader;
        
        Required = ReadRequired() switch
        {
            true => true,
            false => throw new InvalidOperationException($"'{Keys.Required}' must be true"),
            null => throw new InvalidOperationException($"'{Keys.Required}' is required")
        };
        Name = ReadName();
        In = ReadIn();
        Schema = ReadSchema();

        AssertLocation(Location.Path);
    }

    internal static PathParameter Parse(JsonNodeReader reader) => new(reader);

    public override string Name { get; protected init; }
    public override string In { get; protected init; }
    public override bool Required { get; protected init; }
    public override Schema? Schema { get; protected init; }

    internal Evaluator GetEvaluator(OpenApiEvaluationContext openApiEvaluationContext)
    {
        return new Evaluator(openApiEvaluationContext.Evaluate(_reader), this);
    }

    internal class Evaluator
    {
        private readonly OpenApiEvaluationContext _openApiEvaluationContext;
        private readonly PathParameter _parameter;

        internal Evaluator(OpenApiEvaluationContext openApiEvaluationContext, PathParameter parameter)
        {
            _openApiEvaluationContext = openApiEvaluationContext;
            _parameter = parameter;
        }
        
        internal void Evaluate(RoutePattern routePattern)
        {
            if (!routePattern.Values.TryGetValue(_parameter.Name, out var value))
            {
                if (_parameter.Required)
                {
                    _openApiEvaluationContext.EvaluateAsRequired(_parameter.Name);
                }
                return;
            }

            _parameter.Schema?.GetEvaluator(_openApiEvaluationContext).Evaluate(JsonValue.Create(value));
        }
    }
}