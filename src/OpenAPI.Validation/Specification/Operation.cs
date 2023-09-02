using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using OpenAPI.Evaluation.Http;

namespace OpenAPI.Evaluation.Specification;

public sealed partial class Operation
{
    private readonly JsonNodeReader _reader;

    private Operation(JsonNodeReader reader)
    {
        _reader = reader;

        if (_reader.TryRead("requestBody", out var requestBodyReader))
        {
            RequestBody = RequestBody.Parse(requestBodyReader);
        }
        if (_reader.TryRead("parameters", out var parametersReader))
        {
            Parameters = Parameters.Parse(parametersReader);
        }
        if (_reader.TryRead("responses", out var responsesReader))
        {
            Responses = Responses.Parse(responsesReader);
        }
        if (_reader.TryRead("servers", out var serversReader))
        {
            Servers = Servers.Parse(serversReader);
        }
    }

    internal static Operation Parse(JsonNodeReader reader) => new(reader);

    public RequestBody? RequestBody { get; }
    public Parameters? Parameters { get; }
    public Responses? Responses { get; }
    public Servers? Servers { get; }

    internal Evaluator GetEvaluator(OpenApiEvaluationContext openApiEvaluationContext,
        RoutePattern routePattern,
        Parameters.Evaluator? pathItemParametersEvaluator) =>
        new(openApiEvaluationContext.Evaluate(_reader), 
            this, 
            routePattern,
            pathItemParametersEvaluator);

    public class Evaluator
    {
        private readonly OpenApiEvaluationContext _openApiEvaluationContext;
        private readonly Operation _operation;
        private readonly RoutePattern _routePattern;
        private readonly Parameters.Evaluator? _pathItemParametersEvaluator;

        internal Evaluator(OpenApiEvaluationContext openApiEvaluationContext, 
            Operation operation,
            RoutePattern routePattern, 
            Parameters.Evaluator? pathItemParametersEvaluator)
        {
            _openApiEvaluationContext = openApiEvaluationContext;
            _operation = operation;
            _routePattern = routePattern;
            _pathItemParametersEvaluator = pathItemParametersEvaluator;
        }

        public bool TryMatchRequestContent(MediaTypeValue mediaType,
            [NotNullWhen(true)] out MediaType.Evaluator? mediaTypeEvaluator)
        {
            mediaTypeEvaluator = null;
            if (_operation.RequestBody == null)
            {
                _openApiEvaluationContext.Results.Fail("Operation does not define a request body");
                return false;
            }

            var requestBodyEvaluator = _operation.RequestBody.GetEvaluator(_openApiEvaluationContext);
            return requestBodyEvaluator.TryMatch(mediaType, out mediaTypeEvaluator);
        }

        public void EvaluateMissingRequestBody()
        {
            if (_operation.RequestBody?.IsRequired ?? false)
            {
                _openApiEvaluationContext.Results.Fail("Request body is required");
            }
        }

        public void EvaluateRequestHeaders(HttpRequestHeaders headers)
        {
            _pathItemParametersEvaluator?.EvaluateHeaders(headers);
            _operation.Parameters?.GetEvaluator(_openApiEvaluationContext).EvaluateHeaders(headers);
        }

        public void EvaluateRequestPathParameters()
        {
            _pathItemParametersEvaluator?.EvaluatePath(_routePattern);
            _operation.Parameters?.GetEvaluator(_openApiEvaluationContext).EvaluatePath(_routePattern);
        }

        public void EvaluateRequestQueryParameters(Uri uri)
        {
            _pathItemParametersEvaluator?.EvaluateQuery(uri);
            _operation.Parameters?.GetEvaluator(_openApiEvaluationContext).EvaluateQuery(uri);
        }

        public void EvaluateRequestCookies(Uri requestUri, HttpRequestHeaders headers)
        {
            _pathItemParametersEvaluator?.EvaluateCookies(requestUri, headers);
            _operation.Parameters?.GetEvaluator(_openApiEvaluationContext).EvaluateCookies(requestUri, headers);
        }

        internal bool TryMatchResponse(int statusCode,
            [NotNullWhen(true)] out Response.Evaluator? responseEvaluator)
        {
            if (_operation.Responses != null)
            {
                return _operation.Responses.GetEvaluator(_openApiEvaluationContext)
                    .TryMatchResponseContent(statusCode, out responseEvaluator);
            }

            _openApiEvaluationContext.Results.Fail("There are no responses defined");
            responseEvaluator = null;
            return false;
        }

        internal bool TryGetServers([NotNullWhen(true)] out Servers.Evaluator? serversEvaluator)
        {
            if (_operation.Servers == null)
            {
                serversEvaluator = null;
                return false;
            }

            serversEvaluator = _operation.Servers.GetEvaluator(_openApiEvaluationContext);
            return true;
        }
    }
}