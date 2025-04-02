using System.Collections.Specialized;
using OpenAPI.Evaluation.Collections;

namespace OpenAPI.Evaluation.Specification;

public sealed class QueryParameter : Parameter
{
    private readonly JsonNodeReader _reader;

    private QueryParameter(JsonNodeReader reader) : base(reader)
    {
        _reader = reader;
        Name = ReadName();
        In = ReadIn();
        AssertLocation(Location.Query);
        Required = ReadRequired() ?? false;
        Style = ReadStyle() ?? Styles.Form;
        AssertStyle(Styles.Form, Styles.SpaceDelimited, Styles.PipeDelimited, Styles.DeepObject);
        Explode = ReadExplode();

        AllowEmptyValue = ReadAllowEmptyValue();
        AllowReserved = ReadAllowReserved();
    }
    private bool ReadAllowEmptyValue()
    {
        if (!_reader.TryRead("allowEmptyValue", out var allowEmptyValueReader))
            return false;

        Annotations.Add(allowEmptyValueReader);
        return allowEmptyValueReader.GetValue<bool>();
    }
    private bool ReadAllowReserved()
    {
        if (!_reader.TryRead("allowReserved", out var allowReservedReader))
            return false;

        Annotations.Add(allowReservedReader);
        return allowReservedReader.GetValue<bool>();
    }

    internal static QueryParameter Parse(JsonNodeReader reader) => new(reader);
    public override string Name { get; protected init; }
    public override string In { get; protected init; }
    public override bool Required { get; protected init; }
    public override string Style { get; protected init; }
    public override bool Explode { get; protected init; }
    public bool AllowEmptyValue { get; private init; }
    public bool AllowReserved { get; private init; }

    internal Evaluator GetEvaluator(OpenApiEvaluationContext openApiEvaluationContext)
    {
        var context = openApiEvaluationContext.Evaluate(_reader);
        context.Results.SetAnnotations(Annotations);
        return new Evaluator(context, this);
    }

    internal sealed class Evaluator : ParameterEvaluator
    {
        private readonly OpenApiEvaluationContext _openApiEvaluationContext;
        private readonly QueryParameter _parameter;

        internal Evaluator(OpenApiEvaluationContext openApiEvaluationContext, QueryParameter parameter) :
            base(openApiEvaluationContext, parameter)
        {
            _openApiEvaluationContext = openApiEvaluationContext;
            _parameter = parameter;
        }

        internal void Evaluate(NameValueCollection queryParameters)
        {
            var stringValues = _parameter.Style switch
            {
                Styles.DeepObject => queryParameters.AllKeys
                    .Where(key => key?.StartsWith($"{_parameter.Name}[") ?? false)
                    .Select(key => $"{key}={queryParameters.GetValues(key)?.FirstOrDefault()}")
                    .ToArray(),
                _ => queryParameters.GetValues(_parameter.Name)
            };

            if (stringValues == null || !stringValues.Any())
            {
                EvaluateRequired();
                return;
            }

            Evaluate(string.Join('&', stringValues));
        }
    }
}