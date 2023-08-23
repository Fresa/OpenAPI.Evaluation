using System.Text.Json.Nodes;

namespace OpenAPI.Validation.Specification;

public sealed class PathParameter : Parameter
{
    private readonly JsonNodeReader _reader;

    private PathParameter(JsonNodeReader reader) : base(reader)
    {
        _reader = reader;
        AssertLocation(Location.Path);
        if (ReadRequired() is null or false)
            throw new InvalidOperationException($"'{Keys.Required}' is required and must be true");
    }

    internal static PathParameter Parse(JsonNodeReader reader) => new(reader);

    public override bool Required => true;

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