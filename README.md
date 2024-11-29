# Payment Gateway

A Payment Gateway exercise in .NET.

## Project Structure
```
src/
    PaymentGateway.Api - ASP.NET Core Web API project
test/
    PaymentGateway.Api.Tests - unit test project, using xUnit
    IntegrationTests - Postman collections for integration tests
imposters/ - contains the bank simulator configuration. Don't change this

.editorconfig - don't change this. It ensures a consistent set of rules for submissions when reformatting code
docker-compose.yml - configures the bank simulator
PaymentGateway.sln - the Visual Studio solution file
```
## Implementation Details and Design Choices
- The solution supports 3 different currency codes: `GBP`, `EUR`, `USD`.
- A successful payment POST requests returns a `201 Created` status, along with a JSON response model.
- A POST request with invalid parameters returns a `400 Bad Request` status, along with a JSON response model containing a `"status": "Rejected"` field, and a list of error messages.
- Using the .NET buit-in logging library `Microsoft.Extensions.Logging`, and has been configured to output to the console and the debug provider.
- The project uses the [FluentValidation](https://docs.fluentvalidation.net/en/latest/) package for validating the payment request model. The reasons for using this instead of the built-in data annotations are:
  - It separates the validation logic from the API model
  - It offers better control over validation rules
  - The validation can be mocked in unit tests, if needed
- Using the .NET built-in library `System.Text.Json` for parsing JSON.
- The JSON naming convention for the Payment Gateway API models is `camelCase`, as it is the most commonly used in the industry.
- The data models used for communicating with the acquiring bank service have `JsonPropertyName` attributes to configure the JSON field names to use the `snake_case` notation.
- The HTTPClient instance used for calling the acquiring bank is instantiated only once per application, in order to allow it to reuse the same connection pool for multiple requests, and also prevent port exhaustion problems.
- The `PaymentsRepository` is a singleton, to ensure that the same instance is used by all requests and that it is preserved for the whole lifetime of the application.

## Local Development

### Prerequisites
You need to have the following installed on your machine:
- .NET SDK 8.0
- Docker
- Visual Studio 2022 (optional)
- Postman (optional, for manually running integration tests) - [download](https://www.postman.com/downloads/)
- Newman (optional, for running integration tests via CLI) - [how to install](https://learning.postman.com/docs/collections/using-newman-cli/installing-running-newman/)

### Start the Bank Simulator
Ensure that Docker is running on your machine. Open your terminal, and navigate to the project's root directory.

To start the simulator, run the following command: 
```
docker-compose up
```

### Run from Visual Studio
Open the `PaymentGateway.sln` solution file in Visual Studio. 

Ensure that the `PaymentGateway.Api` project is configured as a startup project. To do this, right click the project, and from the context menu select the option `Set as Startup Project`. This will ensure that this project will run automatically when running the solution.

In the the top menu, click on `Debug > Start Debugging` or `Debug > Start Without Debugging` depending on your preference, or use one of the corresponding toolbar buttons.

### Run from Terminal
In your terminal, navigate to the project's root directory.
```
dotnet run --project src/PaymentGateway.Api
```

### Calling the locally running API
The project runs on the following ports: `7092` for HTTPS, and `5067` for HTTP.

The Payments API endpoint will be available at the following URL:
```
https://localhost:7092/api/payments
```

If you have Postman installed, you can import the following collection:
`test/IntegrationTests/PaymentsGateway.postman_collection.json`

This has a several requests that verify the main requirements of the project. Ensure that you run the `POST` requests first, because the `GET` requests depend on them.

You can run the requests individually or in bulk by using the `Run collection` option.

## Testing

### Unit Testing

### Option 1: Run from Visual Studio
With the solution open in Visual Studio, open the Test Explorer panel and click on the `Run All Tests In View` button.

### Option 2: Run from Terminal
In your terminal, navigate to the project's root directory.

```
# list the unit tests:
dotnet test test/PaymentGateway.Api.Tests

# run all the unit tests:
dotnet test --list-tests test/PaymentGateway.Api.Tests
```

### Integration Testing
Required: Newman CLI tool (more details in Prerequisites section)

First, start running the bank simulator and your project, as described in the above sections.

In your terminal, navigate to the project's root directory.
```
# Verifying that the bank simulator is running as expected
newman run -k test/IntegrationTests/PaymentsBankSimulator.postman_collection.json

# Integration tests for the Payment Gateway
newman run -k test/IntegrationTests/PaymentsGateway.postman_collection.json

```

Using the `-k` option disables SSL verification checks and allows self-signed SSL certificates, which is necessary when calling the localhost API URL on HTTPS.