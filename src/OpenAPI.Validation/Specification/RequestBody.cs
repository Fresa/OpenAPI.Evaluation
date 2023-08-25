using System.Diagnostics.CodeAnalysis;
using OpenAPI.Validation.Http;

namespace OpenAPI.Validation.Specification;

public sealed partial class RequestBody
{
    private readonly JsonNodeReader _reader;

    internal RequestBody(JsonNodeReader reader)
    {
        _reader = reader;
        
        var contentReader = _reader.Read("content");
        Content = RequestBodyContent.Parse(contentReader);

        IsRequired = _reader.TryRead("required", out var requiredReader) && 
                     requiredReader.GetValue<bool>();
    }

    internal static RequestBody Parse(JsonNodeReader reader) => new(reader);

    public RequestBodyContent Content { get; }
    public bool IsRequired { get; }

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

        internal bool TryMatch(MediaTypeValue mediaType,
            [NotNullWhen(true)] out MediaType.Evaluator? mediaTypeEvaluator)
        {
            return _requestBody.Content.GetEvaluator(_openApiEvaluationContext)
                .TryMatch(mediaType, out mediaTypeEvaluator);
        }
    }
}