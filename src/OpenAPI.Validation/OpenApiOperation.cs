using System.Diagnostics.CodeAnalysis;
using System.Net;
using Json.Pointer;
using Json.Schema;

namespace OpenAPI.Validation;

public sealed class OpenApiOperation
{
    private readonly JsonNodeReader _operationNodeReader;
    private readonly JsonNodeBaseDocument _baseDocument;
    private readonly RoutePattern _routePattern;
    private readonly EvaluationOptions _evaluationOptions;

    internal OpenApiOperation(
        JsonNodeReader operationNodeReader, 
        JsonNodeBaseDocument baseDocument, 
        RoutePattern routePattern,
        EvaluationOptions evaluationOptions)
    {
        _operationNodeReader = operationNodeReader;
        _baseDocument = baseDocument;
        _routePattern = routePattern;
        _evaluationOptions = evaluationOptions;
    }
    
    public bool TryGetResponseSpecification(HttpStatusCode statusCode, [NotNullWhen(true)] out OpenApiOperationResponse? response)
    {
        var responsesReader = _operationNodeReader.Read("responses");
        if (responsesReader.TryRead(JsonPointer.Create(PointerSegment.Create(((int)statusCode).ToString())),
                out var responseReader))
        {
            response = new OpenApiOperationResponse(new OpenApiEvaluationContext(_baseDocument, responseReader), _evaluationOptions);
            return true;
        }

        response = null;
        return false;
    }
}