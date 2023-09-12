using System.Collections.Specialized;
using System.Text.Json.Nodes;
using OpenAPI.Evaluation.Collections;
using OpenAPI.Evaluation.Http;

namespace OpenAPI.Evaluation.Specification;

public sealed class QueryParameter : Parameter
{
    private readonly JsonNodeReader _reader;

    private QueryParameter(JsonNodeReader reader) : base(reader)
    {
        _reader = reader;
        Name = ReadName();
        In = ReadIn();
        Required = ReadRequired() ?? false;
        
        AssertLocation(Location.Query);
        AssertStyle(Styles.Form, Styles.SpaceDelimited, Styles.PipeDelimited, Styles.DeepObject);

        AllowEmptyValue = ReadAllowEmptyValue();
    }
    private bool ReadAllowEmptyValue()
    {
        if (!_reader.TryRead("allowEmptyValue", out var allowEmptyValueReader))
            return false;

        Annotations.Add(allowEmptyValueReader);
        return allowEmptyValueReader.GetValue<bool>();
    }

    internal static QueryParameter Parse(JsonNodeReader reader) => new(reader);
    public override string Name { get; protected init; }
    public override string In { get; protected init; }
    public override bool Required { get; protected init; }
    public bool AllowEmptyValue { get; private init; }

    internal Evaluator GetEvaluator(OpenApiEvaluationContext openApiEvaluationContext)
    {
        var context = openApiEvaluationContext.Evaluate(_reader);
        context.Results.SetAnnotations(Annotations);
        return new Evaluator(context, this);
    }

    internal sealed class Evaluator
    {
        private readonly OpenApiEvaluationContext _openApiEvaluationContext;
        private readonly QueryParameter _parameter;

        internal Evaluator(OpenApiEvaluationContext openApiEvaluationContext, QueryParameter parameter)
        {
            _openApiEvaluationContext = openApiEvaluationContext;
            _parameter = parameter;
        }

        internal void Evaluate(NameValueCollection queryParameters)
        {
            var stringValues = queryParameters.GetValues(_parameter.Name);
            if (stringValues == null || !stringValues.Any())
            {
                if (_parameter.Required)
                {
                    _openApiEvaluationContext.Results.Fail($"Parameter '{_parameter.Name}' is required");
                }
                return;
            }

            _parameter.Schema?.GetEvaluator(_openApiEvaluationContext).Evaluate(stringValues);

            if (_parameter.Content != null &&
                _parameter.Content.GetEvaluator(_openApiEvaluationContext)
                    .TryMatch(MediaTypeValue.ApplicationJson, out var contentEvaluator))
            {
                var node = JsonNode.Parse(stringValues.First());
                contentEvaluator.Evaluate(node);
            }
        }
    }
}