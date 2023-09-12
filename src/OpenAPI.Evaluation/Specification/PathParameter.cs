using OpenAPI.Evaluation.Http;
using System.Text.Json.Nodes;

namespace OpenAPI.Evaluation.Specification;

public sealed class PathParameter : Parameter
{
    private readonly JsonNodeReader _reader;

    private PathParameter(JsonNodeReader reader) : base(reader)
    {
        _reader = reader;
        Name = ReadName();
        In = ReadIn();
        Required = ReadRequired() switch
        {
            true => true,
            false => throw new ArgumentException($"'{Keys.Required}' must be true"),
            null => throw new ArgumentException($"'{Keys.Required}' is required")
        };
        
        AssertLocation(Location.Path);
    }

    internal static PathParameter Parse(JsonNodeReader reader) => new(reader);

    public override string Name { get; protected init; }
    public override string In { get; protected init; }
    public override bool Required { get; protected init; }
    
    internal Evaluator GetEvaluator(OpenApiEvaluationContext openApiEvaluationContext)
    {
        var context = openApiEvaluationContext.Evaluate(_reader);
        context.Results.SetAnnotations(Annotations);
        return new Evaluator(context, this);
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
                    _openApiEvaluationContext.Results.Fail($"Parameter '{_parameter.Name}' is required");
                }
                return;
            }

            _parameter.Schema?.GetEvaluator(_openApiEvaluationContext).Evaluate(JsonValue.Create(value));

            if (_parameter.Content != null &&
                _parameter.Content.GetEvaluator(_openApiEvaluationContext)
                    .TryMatch(MediaTypeValue.ApplicationJson, out var contentEvaluator))
            {
                var node = JsonNode.Parse(value);
                contentEvaluator.Evaluate(node);
            }
        }
    }
}