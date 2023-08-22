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
        foreach (var mediaTypeReader in contentReader.ReadChildren())
        {
            _content.Add(
                new MediaTypeRange(MediaTypeValue.Parse(mediaTypeReader.Key)), 
                MediaType.Parse(mediaTypeReader));
        }

        _content = _content
            .OrderByDescending(pair => pair.Key.Precedence)
            .ToDictionary(pair => pair.Key, pair => pair.Value);
    }

    internal static RequestBody Parse(JsonNodeReader reader)
    {
        return new RequestBody(reader);
    }

    private readonly Dictionary<MediaTypeRange, MediaType> _content = new();
    public IReadOnlyDictionary<MediaTypeRange, MediaType> Content => _content.AsReadOnly();
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
            foreach (var (mediaTypeRange, mediaTypeItem) in _requestBody.Content)
            {
                if (!mediaTypeRange.Matches(mediaType)) 
                    continue;

                mediaTypeEvaluator = mediaTypeItem.GetEvaluator(_openApiEvaluationContext);
                return true;
            }

            _openApiEvaluationContext.Results.Fail(
                $"Request content media type '{mediaType}' does not match any of the defined media type ranges {string.Join(", ", _requestBody.Content.Keys)}");
            mediaTypeEvaluator = null;
            return false;
        }
    }
}