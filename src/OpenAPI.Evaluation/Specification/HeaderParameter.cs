using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;

namespace OpenAPI.Evaluation.Specification;

public sealed class HeaderParameter : Parameter
{
    private readonly JsonNodeReader _reader;

    private HeaderParameter(JsonNodeReader reader) : base(reader)
    {
        _reader = reader;
        Name = ReadName();
        In = ReadIn();
        AssertLocation(Location.Header);
        Required = ReadRequired() ?? false;
        Style = ReadStyle() ?? Styles.Simple;
        AssertStyle(Styles.Simple);
        Explode = ReadExplode();
    }

    private HeaderParameter(JsonNodeReader reader, string name) : base(reader)
    {
        _reader = reader;
        Name = name;
        In = Location.Header;
        Required = ReadRequired() ?? false;
        Style = ReadStyle() ?? Styles.Simple;
        AssertStyle(Styles.Simple);
    }

    private static readonly string[] IgnoredRequestHeaders = { "Accept", "Content-Type", "Authorization" };
    internal static bool TryParseRequestHeader(JsonNodeReader reader, [NotNullWhen(true)] out HeaderParameter? parameter)
    {
        parameter = new HeaderParameter(reader);
        var ignore = IgnoredRequestHeaders.Contains(parameter.Name, StringComparer.InvariantCultureIgnoreCase);
        if (ignore)
            parameter = null;
        return !ignore;
    }

    private static readonly string[] IgnoredResponseHeaders = { "Content-Type" };
    internal static bool TryParseResponseHeader(JsonNodeReader reader, string name, [NotNullWhen(true)] out HeaderParameter? parameter)
    {
        parameter = new HeaderParameter(reader, name);
        var ignore = IgnoredResponseHeaders.Contains(parameter.Name, StringComparer.InvariantCultureIgnoreCase);
        if (ignore)
            parameter = null;
        return !ignore;
    }

    public override string Name { get; protected init; }
    public override string In { get; protected init; }
    public override bool Required { get; protected init; }
    public override string Style { get; protected init; }
    public override bool Explode { get; protected init; }

    internal Evaluator GetEvaluator(OpenApiEvaluationContext openApiEvaluationContext)
    {
        var context = openApiEvaluationContext.Evaluate(_reader);
        context.Results.SetAnnotations(Annotations);
        return new Evaluator(context, this);
    }

    internal class Evaluator : ParameterEvaluator
    {
        private readonly HeaderParameter _parameter;

        internal Evaluator(OpenApiEvaluationContext openApiEvaluationContext, HeaderParameter parameter) :
            base(openApiEvaluationContext, parameter)
        {
            _parameter = parameter;
        }
        
        internal void Evaluate(IDictionary<string, IEnumerable<string>> headers)
        {
            if (!headers.TryGetValue(_parameter.Name, out var stringValues))
            {
                EvaluateRequired();
                return;
            }

            Evaluate(stringValues.ToArray());
        }
    }
}