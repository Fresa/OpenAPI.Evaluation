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
        AssertLocation(Location.Path);
        Required = ReadRequired() switch
        {
            true => true,
            false => throw new ArgumentException($"'{Keys.Required}' must be true"),
            null => throw new ArgumentException($"'{Keys.Required}' is required")
        };
        Style = ReadStyle() ?? Styles.Simple;
        AssertStyle(Styles.Matrix, Styles.Label, Styles.Simple);
        Explode = ReadExplode();
    }

    internal static PathParameter Parse(JsonNodeReader reader) => new(reader);

    public override string Name { get; protected init; }
    public override string In { get; protected init; }
    public override bool Required { get; protected init; }
    public override string Style { get; protected init; }
    public override bool Explode { get; protected init; }

    internal Evaluator GetEvaluator(OpenApiEvaluationContext openApiEvaluationContext)
    {
        var context = openApiEvaluationContext.Evaluate(_reader);
        context.Results.SetAnnotations(Annotations);
        return new Evaluator(context, this);
    }

    internal class Evaluator : ParameterEvaluator
    {
        private readonly PathParameter _parameter;

        internal Evaluator(OpenApiEvaluationContext openApiEvaluationContext, PathParameter parameter) :
            base(openApiEvaluationContext, parameter)
        {
            _parameter = parameter;
        }

        internal void Evaluate(RoutePattern routePattern)
        {
            if (!routePattern.Values.TryGetValue(_parameter.Name, out var value))
            {
                EvaluateRequired();
                return;
            }

            Evaluate(value);
        }
    }
}