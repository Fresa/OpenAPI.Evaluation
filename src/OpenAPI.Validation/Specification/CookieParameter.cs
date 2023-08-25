using System.Net;
using System.Text.Json.Nodes;

namespace OpenAPI.Validation.Specification;

public sealed class CookieParameter : Parameter
{
    private readonly JsonNodeReader _reader;

    private CookieParameter(JsonNodeReader reader) : base(reader)
    {
        _reader = reader;
        AssertLocation(Location.Cookie);
        Required = ReadRequired() ?? false;
    }

    internal static CookieParameter Parse(JsonNodeReader reader) => new(reader);

    public override bool Required { get; }

    internal Evaluator GetEvaluator(OpenApiEvaluationContext openApiEvaluationContext)
    {
        return new Evaluator(openApiEvaluationContext.Evaluate(_reader), this);
    }

    internal class Evaluator
    {
        private readonly OpenApiEvaluationContext _openApiEvaluationContext;
        private readonly CookieParameter _parameter;

        internal Evaluator(OpenApiEvaluationContext openApiEvaluationContext, CookieParameter parameter)
        {
            _openApiEvaluationContext = openApiEvaluationContext;
            _parameter = parameter;
        }

        internal void Evaluate(CookieCollection cookieCollection)
        {
            var cookie = cookieCollection[_parameter.Name];
            if (cookie == null)
            {
                if (_parameter.Required)
                {
                    _openApiEvaluationContext.EvaluateAsRequired(_parameter.Name);
                }
                return;
            }

            var cookieValue = JsonValue.Create(cookie.Value);
            _parameter.Schema?.GetEvaluator(_openApiEvaluationContext).Evaluate(cookieValue);
        }
    }
}