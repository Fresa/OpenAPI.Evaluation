using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using OpenAPI.Evaluation.Collections;
using OpenAPI.Evaluation.Http;

namespace OpenAPI.Evaluation.Specification;

public sealed class RequestBody
{
    private readonly JsonNodeReader _reader;
    private readonly IDictionary<string, JsonNode?> _annotations = new Dictionary<string, JsonNode?>();

    private RequestBody(JsonNodeReader reader)
    {
        _reader = reader;

        if (_reader.TryRead("description", out var descriptionReader))
        {
            _annotations.Add(descriptionReader);
            Description = descriptionReader.GetValue<string>();
        }

        var contentReader = _reader.Read("content");
        Content = Content.Parse(contentReader);

        IsRequired = _reader.TryRead("required", out var requiredReader) && 
                     requiredReader.GetValue<bool>();
    }
    
    internal static RequestBody Parse(JsonNodeReader reader) => new(reader);

    public string? Description { get; }
    public Content Content { get; }
    public bool IsRequired { get; }

    internal Evaluator GetEvaluator(OpenApiEvaluationContext openApiEvaluationContext)
    {
        var context = openApiEvaluationContext.Evaluate(_reader);
        context.Results.SetAnnotations(_annotations);
        return new Evaluator(context, this);
    }

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

        internal void EvaluateMissingRequestBody()
        {
            if (_requestBody.IsRequired)
            {
                _openApiEvaluationContext.Results.Fail("Request body is required");
            }
        }
    }
}