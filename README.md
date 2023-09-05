# OpenAPI.Evaluation
Evaluates API requests and responses against [OpenAPI 3.1 specifications](https://spec.openapis.org/oas/v3.1.0#openapi-specification) using a custom `System.Net.Http.DelegatingHandler`.

[![Continuous Delivery](https://github.com/Fresa/OpenAPI.Evaluation/actions/workflows/ci.yml/badge.svg)](https://github.com/Fresa/OpenAPI.Evaluation/actions/workflows/ci.yml)

## Installation
```Shell
dotnet add package OpenAPI.Evaluation
```

https://www.nuget.org/packages/OpenAPI.Evaluation/

## Getting Started
Load the OpenAPI specification and create a `HttpClient`.
```dotnet
var stream = File.OpenRead("path/to/openapi-specification.json");
var document = JsonNode.Parse(stream);               
var specification = Specification.OpenAPI.Parse(document);
var evaluationOptions = new EvaluationOptions();
var client = new HttpClient(
    new EvaluationHandler(
        specification,
        evaluationOptions,
        new HttpClientHandler()));
``` 

When sending a request the `HttpResponseMessage` will be wrapped by a `EvaluationHttpResponseMessage` that contains the evaluation results in the property `EvaluationResults`.
If the `HttpRequestMessage` fails evaluation the handler will return a `EvaluationHttpResponseMessage` with a `BadRequest` response and never send the request to the server.

If you rather prefer failed evaluations to throw and exception, this is configurable in the `EvaluationOptions`.

### Yaml
To load a yaml formatted specification, I recommend [Yaml2JsonNode](https://www.nuget.org/packages/Yaml2JsonNode/).
```dotnet
var yaml = new YamlStream();
using var reader = File.OpenText("path/to/openapi-specification.yaml");
yaml.Load(reader);
var document = yaml.ToJsonNode().First();
```

# Contributing
Pull requests are welcome. For major changes, please open an issue first to discuss what you would like to change.

Please make sure to update tests as appropriate.

# License
[MIT](LICENSE)