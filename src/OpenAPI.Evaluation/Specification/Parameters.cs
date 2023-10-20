using System.Collections;
using System.Net;
using System.Net.Http.Headers;

namespace OpenAPI.Evaluation.Specification;

public sealed partial class Parameters : IEnumerable<Parameter>
{
    private readonly JsonNodeReader _reader;
    private readonly IEnumerable<Parameter> _parameters;

    private Parameters(JsonNodeReader reader, IEnumerable<Parameter> parameters)
    {
        _reader = reader;
        _parameters = parameters;
    }
    
    private static IEnumerable<Parameter> ReadParameters(JsonNodeReader reader)
    {
        var readParameters = new List<Parameter>();
        foreach (var parameterReader in reader.ReadChildren())
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

    private readonly UniqueParameterComparer _uniqueParameterComparer = new();
    internal Parameters Except(Parameters parameters) => 
        new(_reader, this.Except(parameters, _uniqueParameterComparer));

    internal static Parameters Parse(JsonNodeReader reader)
    {
        var parameters = ReadParameters(reader);
        return new(reader, parameters);
    }

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

        internal void EvaluateHeaders(IDictionary<string, IEnumerable<string>> requestHeaders)
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
            var queryParameters = System.Web.HttpUtility.ParseQueryString(querystring);

            foreach (var parameter in _parameters.OfType<QueryParameter>())
            {
                parameter.GetEvaluator(_openApiEvaluationContext).Evaluate(queryParameters);
            }
        }

        internal void EvaluateCookies(Uri requestUri, IDictionary<string, IEnumerable<string>> requestHeaders)
        {
            if (!requestHeaders.TryGetValue("Cookie", out var cookieValueList))
            {
                cookieValueList = new List<string>();
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

    private class UniqueParameterComparer : IEqualityComparer<Parameter>
    {
        public bool Equals(Parameter? x, Parameter? y) =>
            x?.Name == y?.Name &&
            x != null && y != null && x.In.Equals(y.In, StringComparison.InvariantCultureIgnoreCase);

        public int GetHashCode(Parameter obj)
        {
            var hash = new HashCode();
            hash.Add(obj.Name);
            hash.Add(obj.In, StringComparer.InvariantCultureIgnoreCase);
            return hash.ToHashCode();
        }
    }
}