using System.Collections;
using System.Net;
using System.Net.Http.Headers;

namespace OpenAPI.Evaluation.Specification;

public sealed partial class Parameters : IEnumerable<Parameter>
{
    private readonly JsonNodeReader _reader;
    private readonly IEnumerable<Parameter> _parameters;

    private Parameters(JsonNodeReader reader, Parameters? baseParameters = null)
    {
        _reader = reader;
        var parameters = ReadParameters();
        if (baseParameters != null)
        {
            foreach (var baseParameter in baseParameters)
            {
                if (!parameters.Exists(readParameter =>
                        readParameter.In == baseParameter.In &&
                        readParameter.Name == baseParameter.Name))
                {
                    parameters.Add(baseParameter);
                }
            }
        }
        _parameters = parameters;
    }

    private List<Parameter> ReadParameters()
    {
        var readParameters = new List<Parameter>();
        foreach (var parameterReader in _reader.ReadChildren())
        {
            if (!Parameter.TryParse(parameterReader, out var parameter))
                continue;
            if (readParameters.Exists(readParameter =>
                    readParameter.In == parameter.In &&
                    readParameter.Name == parameter.Name))
            {
                throw new InvalidOperationException(
                    $"Parameter with name {parameter.Name} and location {parameter.In} can only appear once");
            }

            readParameters.Add(parameter);
        }

        return readParameters;
    }

    internal static Parameters Parse(JsonNodeReader reader, Parameters? baseParameters = null) => new(reader, baseParameters);

    public IEnumerator<Parameter> GetEnumerator() => _parameters.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    internal Evaluator GetEvaluator(OpenApiEvaluationContext openApiEvaluationContext)
    {
        return new Evaluator(openApiEvaluationContext.Evaluate(_reader), this);
    }

    internal class Evaluator
    {
        private readonly OpenApiEvaluationContext _openApiEvaluationContext;
        private readonly Parameters _parameters;

        internal Evaluator(OpenApiEvaluationContext openApiEvaluationContext, Parameters parameters)
        {
            _openApiEvaluationContext = openApiEvaluationContext;
            _parameters = parameters;
        }

        internal void EvaluateHeaders(HttpRequestHeaders requestHeaders)
        {
            foreach (var parameter in _parameters.OfType<HeaderParameter>())
            {
                parameter.GetEvaluator(_openApiEvaluationContext).Evaluate(requestHeaders);
            }
        }

        internal void EvaluatePath(RoutePattern routePattern)
        {
            foreach (var parameter in _parameters.OfType<PathParameter>())
            {
                parameter.GetEvaluator(_openApiEvaluationContext).Evaluate(routePattern);
            }
        }

        internal void EvaluateQuery(Uri uri)
        {
            var querystring = uri.Query;
            if (string.IsNullOrEmpty(querystring))
            {
                return;
            }
            var queryParameters = System.Web.HttpUtility.ParseQueryString(querystring);

            foreach (var parameter in _parameters.OfType<QueryParameter>())
            {
                parameter.GetEvaluator(_openApiEvaluationContext).Evaluate(queryParameters);
            }
        }

        internal void EvaluateCookies(Uri requestUri, HttpRequestHeaders requestHeaders)
        {
            if (!requestHeaders.TryGetValues("Cookie", out var cookieValueList))
            {
                return;
            }

            var cookieContainer = new CookieContainer();
            foreach (var value in cookieValueList)
            {
                cookieContainer.SetCookies(requestUri, value);
            }
            var cookies = cookieContainer.GetCookies(requestUri);

            foreach (var parameter in _parameters.OfType<CookieParameter>())
            {
                parameter.GetEvaluator(_openApiEvaluationContext).Evaluate(cookies);
            }
        }
    }
}