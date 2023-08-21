using System.Diagnostics.CodeAnalysis;

namespace OpenAPI.Validation.Specification;

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