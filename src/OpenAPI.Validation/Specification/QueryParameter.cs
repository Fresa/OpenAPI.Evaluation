using System.Collections.Specialized;

namespace OpenAPI.Evaluation.Specification;

public sealed class QueryParameter : Parameter
{
    private readonly JsonNodeReader _reader;

    private QueryParameter(JsonNodeReader reader) : base(reader)
    {
        _reader = reader;
        Required = ReadRequired() ?? false;
        Name = ReadName();
        In = ReadIn();
        Schema = ReadSchema();
        Description = ReadDescription();

        AssertLocation(Location.Query);
    }

    internal static QueryParameter Parse(JsonNodeReader reader) => new(reader);
    public override string Name { get; protected init; }
    public override string In { get; protected init; }
    public override bool Required { get; protected init; }
    public override Schema? Schema { get; protected init; }
    public override string? Description { get; protected init; }

    internal Evaluator GetEvaluator(OpenApiEvaluationContext openApiEvaluationContext)
    {
        var context = openApiEvaluationContext.Evaluate(_reader);
        context.Results.SetAnnotations(Annotations);
        return new Evaluator(context, this);
    }

    internal class Evaluator
    {
        private readonly OpenApiEvaluationContext _openApiEvaluationContext;
        private readonly QueryParameter _parameter;

        internal Evaluator(OpenApiEvaluationContext openApiEvaluationContext, QueryParameter parameter)
        {
            _openApiEvaluationContext = openApiEvaluationContext;
            _parameter = parameter;
        }

        internal void Evaluate(NameValueCollection queryParameters)
        {
            var stringValues = queryParameters.GetValues(_parameter.Name);
            if (stringValues == null)
            {
                if (_parameter.Required)
                {
                    _openApiEvaluationContext.Results.Fail($"Parameter '{_parameter.Name}' is required");
                }
                return;
            }

            _parameter.Schema?.GetEvaluator(_openApiEvaluationContext).Evaluate(stringValues);
        }
    }
}