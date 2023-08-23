using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;

namespace OpenAPI.Validation.Specification;

public sealed class HeaderParameter : Parameter
{
    private readonly JsonNodeReader _reader;

    private HeaderParameter(JsonNodeReader reader) : base(reader)
    {
        _reader = reader;
        AssertLocation(Location.Header);
        Required = ReadRequired() ?? false;
    }

    private static readonly string[] IgnoredParameterNames = { "Accept", "Content-Type", "Authorization" };
    internal static bool TryParse(JsonNodeReader reader, [NotNullWhen(true)] out HeaderParameter? parameter)
    {
        parameter = new HeaderParameter(reader);
        var ignore = IgnoredParameterNames.Contains(parameter.Name, StringComparer.InvariantCultureIgnoreCase);
        if (ignore)
            parameter = null;
        return !ignore;
    }

    public override bool Required { get; }

    internal Evaluator GetEvaluator(OpenApiEvaluationContext openApiEvaluationContext)
    {
        return new Evaluator(openApiEvaluationContext.Evaluate(_reader), this);
    }

    internal class Evaluator
    {
        private readonly OpenApiEvaluationContext _openApiEvaluationContext;
        private readonly HeaderParameter _parameter;

        internal Evaluator(OpenApiEvaluationContext openApiEvaluationContext, HeaderParameter parameter)
        {
            _openApiEvaluationContext = openApiEvaluationContext;
            _parameter = parameter;
        }

        internal void Evaluate(HttpRequestHeaders requestHeaders)
        {
            if (!requestHeaders.TryGetValues(_parameter.Name, out var stringValues))
            {
                if (_parameter.Required)
                {
                    _openApiEvaluationContext.EvaluateAsRequired(_parameter.Name);
                }
                return;
            }

            _parameter.Schema?.GetEvaluator(_openApiEvaluationContext).Evaluate(stringValues);
        }
    }
}