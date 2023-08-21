using Json.Pointer;
using Json.Schema;
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

        internal void EvaluatePathParameters()
        {
            // todo: route pattern validation against path parameters
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

        public void EvaluateRequestContent(JsonNode requestContent)
        {
            throw new NotImplementedException();
        }

        public void EvaluateRequestHeaders(HttpRequestHeaders requestHeaders)
        {
            throw new NotImplementedException();
        }

        public void EvaluateRequestPathParameters()
        {
            throw new NotImplementedException();
        }

        public void EvaluateRequestQueryParameters(Uri requestRequestUri)
        {
            throw new NotImplementedException();
        }
    }
}

public sealed partial class RequestBody
{
    private readonly JsonNodeReader _reader;

    internal RequestBody(JsonNodeReader reader)
    {
        _reader = reader;

        var contentReader = _reader.Read("content");
        foreach (var mediaTypeReader in contentReader.ReadChildren())
        {
            _content.Add(mediaTypeReader.Key, MediaType.Parse(mediaTypeReader));
        }

    }
    internal static RequestBody Parse(JsonNodeReader reader)
    {
        return new RequestBody(reader);
    }

    private readonly Dictionary<string, MediaType> _content = new();
    public IReadOnlyDictionary<string, MediaType> Content => _content.AsReadOnly();
    internal Evaluator GetEvaluator(OpenApiEvaluationContext openApiEvaluationContext) =>
        new(openApiEvaluationContext.Evaluate(_reader), this);

    public class Evaluator
    {
        private readonly OpenApiEvaluationContext _openApiEvaluationContext;
        private readonly RequestBody _requestBody;

        internal Evaluator(OpenApiEvaluationContext openApiEvaluationContext, RequestBody requestBody)
        {
            _openApiEvaluationContext = openApiEvaluationContext;
            _requestBody = requestBody;
        }

        internal bool TryMatch(string mediaType,
            [NotNullWhen(true)] out MediaType? pathItem)
        {
            pathItem = null;
            return false;
        }
    }
}

public sealed partial class MediaType
{
    private readonly JsonNodeReader _reader;

    internal MediaType(JsonNodeReader reader)
    {
        _reader = reader;

        if (_reader.TryRead("schema", out var schemaReader))
        {
            Schema = schemaReader.RootPath;
        }
    }

    internal static MediaType Parse(JsonNodeReader reader)
    {
        return new MediaType(reader);
    }

    public JsonPointer? Schema { get; }

    public class Evaluator
    {
        private readonly OpenApiEvaluationContext _openApiEvaluationContext;
        private readonly MediaType _mediaType;

        internal Evaluator(OpenApiEvaluationContext openApiEvaluationContext, MediaType mediaType)
        {
            _openApiEvaluationContext = openApiEvaluationContext;
            _mediaType = mediaType;
        }

        public void EvaluateBody(JsonNode? body)
        {
            // todo
        }
    }
}