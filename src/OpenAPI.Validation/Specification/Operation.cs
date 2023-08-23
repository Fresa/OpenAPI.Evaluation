using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Text.Json.Nodes;
using OpenAPI.Validation.Http;

namespace OpenAPI.Validation.Specification;

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
    }

    internal static Operation Parse(JsonNodeReader reader)
    {
        return new Operation(reader);
    }

    public RequestBody? RequestBody { get; }

    public Parameters? Parameters { get; }

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
            _operation = operation;
            _routePattern = routePattern;
        }
        
        internal bool TryMatchRequestContent(MediaTypeValue mediaType,
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

        public void EvaluateRequestHeaders(HttpRequestHeaders headers)
        {
            _operation.Parameters?.GetEvaluator(_openApiEvaluationContext).EvaluateHeaders(headers);
        }
        
        public void EvaluateRequestPathParameters()
        {
            _operation.Parameters?.GetEvaluator(_openApiEvaluationContext).EvaluatePath(_routePattern);
        }

        public void EvaluateRequestQueryParameters(Uri uri)
        {
            _operation.Parameters?.GetEvaluator(_openApiEvaluationContext).EvaluateQuery(uri);
        }
    }
}