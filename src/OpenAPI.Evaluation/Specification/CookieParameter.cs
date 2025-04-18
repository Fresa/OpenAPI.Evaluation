using System.Net;

namespace OpenAPI.Evaluation.Specification;

public sealed class CookieParameter : Parameter
{
    private readonly JsonNodeReader _reader;

    private CookieParameter(JsonNodeReader reader) : base(reader)
    {
        _reader = reader;
        Name = ReadName();
        In = ReadIn();
        AssertLocation(Location.Cookie);
        Required = ReadRequired() ?? false;

        Style = ReadStyle() ?? Styles.Form;
        AssertStyle(Styles.Form);
        Explode = ReadExplode();
    }

    internal static CookieParameter Parse(JsonNodeReader reader) => new(reader);

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
        private readonly CookieParameter _parameter;

        internal Evaluator(OpenApiEvaluationContext openApiEvaluationContext, CookieParameter parameter) :
            base(openApiEvaluationContext, parameter)
        {
            _parameter = parameter;
        }

        internal void Evaluate(CookieCollection cookieCollection)
        {
            var cookie = cookieCollection[_parameter.Name];
            if (cookie == null)
            {
                EvaluateRequired();
                return;
            }

            Evaluate(cookie.Value);
        }
    }
}