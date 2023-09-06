using System.Collections;
using System.Diagnostics.CodeAnalysis;
using OpenAPI.Evaluation.Http;

namespace OpenAPI.Evaluation.Specification;

public sealed class Content : IReadOnlyDictionary<MediaTypeRange, MediaType>
{
    private readonly JsonNodeReader _reader;

    internal Content(JsonNodeReader reader)
    {
        _reader = reader;

        foreach (var mediaTypeReader in reader.ReadChildren())
        {
            _content.Add(
                new MediaTypeRange(MediaTypeValue.Parse(mediaTypeReader.Key)),
                MediaType.Parse(mediaTypeReader));
        }

        _content = _content
            .OrderByDescending(pair => pair.Key.Precedence)
            .ToDictionary(pair => pair.Key, pair => pair.Value);
    }

    internal static Content Parse(JsonNodeReader reader)
    {
        return new Content(reader);
    }

    private readonly Dictionary<MediaTypeRange, MediaType> _content = new();

    public IEnumerator<KeyValuePair<MediaTypeRange, MediaType>> GetEnumerator() => _content.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public int Count => _content.Count;
    public bool ContainsKey(MediaTypeRange key) => _content.ContainsKey(key);

    public bool TryGetValue(MediaTypeRange key, [MaybeNullWhen(false)] out MediaType value) => _content.TryGetValue(key, out value);

    public MediaType this[MediaTypeRange key] => _content[key];

    public IEnumerable<MediaTypeRange> Keys => _content.Keys;
    public IEnumerable<MediaType> Values => _content.Values;

    internal Evaluator GetEvaluator(OpenApiEvaluationContext openApiEvaluationContext) =>
        new(openApiEvaluationContext.Evaluate(_reader), this);

    public class Evaluator
    {
        private readonly OpenApiEvaluationContext _openApiEvaluationContext;
        private readonly Content _content;

        internal Evaluator(OpenApiEvaluationContext openApiEvaluationContext, Content content)
        {
            _openApiEvaluationContext = openApiEvaluationContext;
            _content = content;
        }

        internal bool TryMatch(MediaTypeValue mediaType,
            [NotNullWhen(true)] out MediaType.Evaluator? mediaTypeEvaluator)
        {
            foreach (var (mediaTypeRange, mediaTypeItem) in _content)
            {
                if (!mediaTypeRange.Matches(mediaType))
                    continue;

                mediaTypeEvaluator = mediaTypeItem.GetEvaluator(_openApiEvaluationContext);
                return true;
            }

            _openApiEvaluationContext.Results.Fail(
                $"Content media type '{mediaType}' does not match any of the defined media type ranges {string.Join(", ", _content.Keys.Select(range => range.ToString()))}");
            mediaTypeEvaluator = null;
            return false;
        }
    }
}