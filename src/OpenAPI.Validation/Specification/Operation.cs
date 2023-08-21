using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Text.Json.Nodes;

namespace OpenAPI.Validation.Specification;

public sealed partial class Operation
{
    private readonly JsonNodeReader _reader;

    private Operation(JsonNodeReader reader)
    {
        _reader = reader;

        if (_reader.TryRead("mediaType", out var requestBodyReader))
        {
            RequestBody = RequestBody.Parse(requestBodyReader);
        }
    }

    internal static Operation Parse(JsonNodeReader reader)
    {
        return new Operation(reader);
    }

    public RequestBody? RequestBody { get; }

    internal Evaluator GetEvaluator(OpenApiEvaluationContext openApiEvaluationContext, RoutePattern routePattern) =>
        new(openApiEvaluationContext.Evaluate(_reader), this, routePattern);

    public class Evaluator
    {
        private readonly OpenApiEvaluationContext _openApiEvaluationContext;
        private readonly Operation _operation;
        private readonly RoutePattern _routePattern;

        internal Evaluator(OpenApiEvaluationContext openApiEvaluationContext, Operation operation,
            RoutePattern routePattern)
        {
            _openApiEvaluationContext = openApiEvaluationContext;
            _openApiEvaluationContext.Results.OneDetail();

            _operation = operation;
            _routePattern = routePattern;
        }
        
        internal bool TryMatch(string mediaType,
            [NotNullWhen(true)] out MediaType? mediaTypeObject)
        {
            mediaTypeObject = null;
            if (_operation.RequestBody == null)
            {
                _openApiEvaluationContext.Results.Fail("Operation does not define a request body");
                return false;
            }

            var requestBodyEvaluator = _operation.RequestBody.GetEvaluator(_openApiEvaluationContext);
            return requestBodyEvaluator.TryMatch(mediaType, out mediaTypeObject);
        }

        public void EvaluateRequestContent(JsonNode? content)
        {
            if (!_openApiEvaluationContext.TryEvaluate("requestBody", out var requestEvaluationContext))
            {
                return;
            }

            var requestBodySchemaEvaluationResults = requestEvaluationContext.Evaluate("content", "application/json", "schema");
            requestBodySchemaEvaluationResults.EvaluateAgainstSchema(content);
        }

        public void EvaluateRequestHeaders(HttpRequestHeaders headers)
        {
            var parametersEvaluationContext = _openApiEvaluationContext.Evaluate("parameters");
            foreach (var parameterEvaluationContext in parametersEvaluationContext.EvaluateChildren())
            {
                if (parameterEvaluationContext.GetValue<string>("in") != "header")
                    continue;

                var name = parameterEvaluationContext.GetValue<string>("name");
                if (!headers.TryGetValues(name, out var stringValues))
                {
                    if (parameterEvaluationContext.TryGetValue<bool>("required", out var required) &&
                        required)
                    {
                        parameterEvaluationContext.EvaluateAsRequired(name);
                    }
                    continue;
                }

                var parameterSchemaEvaluationResults = parameterEvaluationContext.Evaluate("schema");
                parameterSchemaEvaluationResults.EvaluateAgainstSchema(stringValues.ToArray());
            }
        }

        public void EvaluateRequestPathParameters()
        {
            var parametersEvaluationContext = _openApiEvaluationContext.Evaluate("parameters");
            foreach (var parameterEvaluationContext in parametersEvaluationContext.EvaluateChildren())
            {
                if (parameterEvaluationContext.GetValue<string>("in") != "path")
                    continue;

                var name = parameterEvaluationContext.GetValue<string>("name");
                if (!_routePattern.Values.TryGetValue(name, out var routeValue))
                {
                    throw new InvalidOperationException(
                        $"The endpoint {_routePattern.Template} is invalid as it does not contain the defined path parameter {name} specified at {parameterEvaluationContext.Results.SpecificationLocation}");
                }

                var parameterSchemaEvaluationResults = parameterEvaluationContext.Evaluate("schema");
                var value = JsonValue.Create(routeValue);
                parameterSchemaEvaluationResults.EvaluateAgainstSchema(value);
            }
        }

        public void EvaluateRequestQueryParameters(Uri uri)
        {
            var querystring = uri.Query;
            if (string.IsNullOrEmpty(querystring))
            {
                return;
            }
            var queryParameters = System.Web.HttpUtility.ParseQueryString(querystring);

            var parametersEvaluationContext = _openApiEvaluationContext.Evaluate("parameters");
            foreach (var parameterEvaluationContext in parametersEvaluationContext.EvaluateChildren())
            {
                if (parameterEvaluationContext.GetValue<string>("in") != "query")
                    continue;

                var name = parameterEvaluationContext.GetValue<string>("name");
                var stringValues = queryParameters.GetValues(name);
                if (stringValues == null)
                {
                    if (parameterEvaluationContext.TryGetValue<bool>("required", out var required) &&
                        required)
                    {
                        parameterEvaluationContext.EvaluateAsRequired(name);
                    }
                    continue;
                }

                var parameterSchemaEvaluationResults = parameterEvaluationContext.Evaluate("schema");
                parameterSchemaEvaluationResults.EvaluateAgainstSchema(stringValues);
            }
        }
    }
}