using System.ComponentModel;
using System.Net;
using System.Net.Http.Json;

using FluentValidation;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using NSubstitute;
using NSubstitute.Equivalency;
using NSubstitute.ExceptionExtensions;

using PaymentGateway.Api.Controllers;
using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;
using PaymentGateway.Api.Validation;

namespace PaymentGateway.Api.Tests;

public class PaymentsControllerTests
{
    private readonly HttpClient _apiClient;
    
    private readonly IValidator<PostPaymentRequest> _validator = new PaymentRequestValidator();
    private readonly IPaymentsRepository _paymentsRepository = new PaymentsRepository();

    private readonly ILogger<PaymentsController> _loggerMock = Substitute.For<ILogger<PaymentsController>>();
    private readonly IAcquiringBankService _acquiringBankServiceMock = Substitute.For<IAcquiringBankService>();

    private readonly Random _random = new();

    public PaymentsControllerTests()
    {
        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        
        _apiClient = webApplicationFactory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services => ((ServiceCollection)services)
                .AddSingleton(_loggerMock)
                .AddSingleton(_validator)
                .AddSingleton(_paymentsRepository)
                .AddSingleton(_acquiringBankServiceMock)))
            .CreateClient();
    }

    private PostPaymentRequest BuildValidPaymentRequest()
    {
        var expiryDate = DateTime.Today.AddYears(2).AddMonths(2);
        return new PostPaymentRequest
        {
            CardNumber = "2222405343248877",
            ExpiryMonth = expiryDate.Month,
            ExpiryYear = expiryDate.Year,
            Currency = "GBP",
            Amount = 1500,
            Cvv = "123"
        };
    }

    #region Testing GET Action

    [Fact(DisplayName = "Retrieves a payment successfully")]
    public async Task RetrievesAPaymentSuccessfully()
    {
        // Arrange
        var payment = new PaymentResponse
        {
            Id = Guid.NewGuid(),
            ExpiryYear = _random.Next(2023, 2030),
            ExpiryMonth = _random.Next(1, 12),
            Amount = _random.Next(1, 10000),
            CardNumberLastFour = _random.Next(1111, 9999).ToString(),
            Currency = "GBP"
        };

        _paymentsRepository.Add(payment);

        // Act
        var response = await _apiClient.GetAsync($"/api/Payments/{payment.Id}");
        var paymentResponse = await response.Content.ReadFromJsonAsync<PaymentResponse>();
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(paymentResponse);
    }

    [Fact(DisplayName = "Returns 404 if payment not found")]
    public async Task Returns404IfPaymentNotFound()
    {
        // Arrange
        
        // Act
        var response = await _apiClient.GetAsync($"/api/Payments/{Guid.NewGuid()}");
        
        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region Testing POST Action

    [Theory(DisplayName = "Processes and authorizes payment")]
    [InlineData("5555555555554444", 1, 2030, "123", 25000, "GBP", "4444")]
    [InlineData("5105105105105100", 2, 2031, "002", 100, "GBP", "5100")]
    [InlineData("4111111111111111", 3, 2032, "5000", 15000, "EUR", "1111")]
    [InlineData("4012888888881881", 4, 2033, "1234", 350000, "EUR", "1881")]
    [InlineData("42222222222222", 5, 2034, "5555", 1234565, "USD", "2222")]
    [InlineData("3530111333300000", 6, 2035, "333", 65656565, "USD", "0000")]
    [InlineData("378282246310005", 7, 2036, "000", 12345678, "GBP", "0005")]
    [InlineData("378734493671000", 8, 2038, "010", 55555555, "EUR", "1000")]
    [InlineData("5610591081018250", 9, 2039, "010", 2222222, "USD", "8250")]
    [InlineData("6011111111111117", 10, 2040, "010", 111111, "GBP", "1117")]
    [InlineData("6011000990139424", 11, 2041, "010", 150000, "EUR", "9424")]
    [InlineData("3530111333300000", 12, 2042, "010", 25000, "USD", "0000")]
    public async Task PostPaymentAsync_Authorized(
        string cardNumber, int expiryMonth, int expiryYear, 
        string cvv, int amount, string currency,
        string expectedLast4Digits)
    {
        // Arrange
        var paymentRequest = new PostPaymentRequest
        {
            CardNumber = cardNumber,
            ExpiryMonth = expiryMonth,
            ExpiryYear = expiryYear,
            Cvv = cvv,
            Amount = amount,
            Currency = currency
        };
        var bankRequest = new BankPaymentRequest(cardNumber, expiryMonth, expiryYear, currency, amount, cvv);
        var dummyBankResponse = new BankPaymentResponse
        {
            AuthorizationCode = Guid.NewGuid().ToString(),
            Authorized = true
        };

        _acquiringBankServiceMock.ProcessPaymentAsync(bankRequest).ReturnsForAnyArgs(dummyBankResponse);


        // Act
        var response = await _apiClient.PostAsJsonAsync($"/api/Payments", paymentRequest);


        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var paymentResponse = await response.Content.ReadFromJsonAsync<PaymentResponse>();
        Assert.NotNull(paymentResponse);
        Assert.Equal(PaymentStatus.Authorized, paymentResponse.Status);
        Assert.NotEqual(Guid.Empty, paymentResponse.Id);
        Assert.Equal(expectedLast4Digits, paymentResponse.CardNumberLastFour);
        Assert.Equal(expiryMonth, paymentResponse.ExpiryMonth);
        Assert.Equal(expiryYear, paymentResponse.ExpiryYear);
        Assert.Equal(currency, paymentResponse.Currency);
        Assert.Equal(amount, paymentResponse.Amount);

        // Check that the bank service received a ProcessPaymentAsync request,
        // with an argument that's equivalent to the expected bankRequest.
        await _acquiringBankServiceMock.Received()
            .ProcessPaymentAsync(ArgEx.IsEquivalentTo(bankRequest));

        // Check that the payment was added to the repository
        var repoPayment = _paymentsRepository.Get(paymentResponse.Id);
        Assert.Equivalent(paymentResponse, repoPayment);
    }

    [Fact(DisplayName = "Processes and declines payment")]
    public async Task PostPaymentAsync_Declined()
    {
        // Arrange
        var paymentRequest = BuildValidPaymentRequest();

        var bankRequest = new BankPaymentRequest(
            paymentRequest.CardNumber, paymentRequest.ExpiryMonth, paymentRequest.ExpiryYear,
            paymentRequest.Currency, paymentRequest.Amount, paymentRequest.Cvv);

        var dummyBankResponse = new BankPaymentResponse { AuthorizationCode = "", Authorized = false };

        _acquiringBankServiceMock.ProcessPaymentAsync(bankRequest).ReturnsForAnyArgs(dummyBankResponse);


        // Act
        var response = await _apiClient.PostAsJsonAsync($"/api/Payments", paymentRequest);


        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var paymentResponse = await response.Content.ReadFromJsonAsync<PaymentResponse>();
        Assert.NotNull(paymentResponse);
        Assert.Equal(PaymentStatus.Declined, paymentResponse.Status);
        Assert.NotEqual(Guid.Empty, paymentResponse.Id);
        Assert.Equal("8877", paymentResponse.CardNumberLastFour);
        Assert.Equal(paymentRequest.ExpiryMonth, paymentResponse.ExpiryMonth);
        Assert.Equal(paymentRequest.ExpiryYear, paymentResponse.ExpiryYear);
        Assert.Equal(paymentRequest.Currency, paymentResponse.Currency);
        Assert.Equal(paymentRequest.Amount, paymentResponse.Amount);

        // Check that the payment was added to the repository
        var repoPayment = _paymentsRepository.Get(paymentResponse.Id);
        Assert.Equivalent(paymentResponse, repoPayment);
        Assert.Equal(PaymentStatus.Declined, repoPayment.Status);
    }

    [Theory(DisplayName = "Payment with invalid params rejected")]
    [InlineData("Small card number", "5445", 1, 2030, "123", 25000, "GBP")]
    [InlineData("Large card number", "54458927498324982734928374928743", 1, 2030, "123", 25000, "GBP")]
    [InlineData("Empty card number", "", 1, 2030, "123", 25000, "GBP")]
    [InlineData("Invalid month", "5555555555554444", 0, 2030, "123", 25000, "GBP")]
    [InlineData("Invalid month", "5555555555554444", -1, 2030, "123", 25000, "GBP")]
    [InlineData("Invalid month", "5555555555554444", 13, 2030, "123", 25000, "GBP")]
    [InlineData("Invalid year", "5555555555554444", 1, 2019, "123", 25000, "GBP")]
    [InlineData("Small CVV", "5555555555554444", 1, 2030, "12", 25000, "GBP")]
    [InlineData("Large CVV", "5555555555554444", 1, 2030, "12345", 25000, "GBP")]
    [InlineData("Zero amount", "5555555555554444", 1, 2030, "123", 0, "GBP")]
    [InlineData("Negative amount", "5555555555554444", 1, 2030, "123", -200, "GBP")]
    [InlineData("Invalid currency", "5555555555554444", 1, 2030, "123", 25000, "GB")]
    [InlineData("Empty currency", "5555555555554444", 1, 2030, "123", 25000, "")]
    [InlineData("Unsupported currency", "5555555555554444", 1, 2030, "123", 25000, "DKK")]
    [InlineData("Multiple violations", "5445", -1, 1999, "123456", -100, "DKK")]
    public async Task PostPaymentAsync_RejectedForInvalidParams(
        string reason,
        string cardNumber, int expiryMonth, int expiryYear,
        string cvv, int amount, string currency)
    {
        // Arrange
        var paymentRequest = new PostPaymentRequest
        {
            CardNumber = cardNumber,
            ExpiryMonth = expiryMonth,
            ExpiryYear = expiryYear,
            Cvv = cvv,
            Amount = amount,
            Currency = currency
        };

        // Act
        var response = await _apiClient.PostAsJsonAsync($"/api/Payments", paymentRequest);

        
        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var validationResponse = await response.Content.ReadFromJsonAsync<PostPaymentFailedValidationResponse>();
        Assert.NotNull(validationResponse);
        Assert.Equal(PaymentStatus.Rejected, validationResponse.Status);
        Assert.NotEmpty(validationResponse.Errors);

        // Check that the bank was not called
        await _acquiringBankServiceMock.DidNotReceive().ProcessPaymentAsync(Arg.Any<BankPaymentRequest>());
    }

    [Fact(DisplayName = "Payment with past expiration date rejected")]
    public async Task PostPaymentAsync_RejectedForPastExpirationDate()
    {
        // The expiry date is one month in the past.
        // This will be in the same year if the Month is not January
        var expiryDate = DateTime.Today.AddMonths(-1);

        var paymentRequest = BuildValidPaymentRequest();
        paymentRequest.ExpiryMonth = expiryDate.Month;
        paymentRequest.ExpiryYear = expiryDate.Year;

        // Act
        var response = await _apiClient.PostAsJsonAsync($"/api/Payments", paymentRequest);


        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var validationResponse = await response.Content.ReadFromJsonAsync<PostPaymentFailedValidationResponse>();
        Assert.NotNull(validationResponse);
        Assert.Equal(PaymentStatus.Rejected, validationResponse.Status);
        Assert.NotEmpty(validationResponse.Errors);

        if (paymentRequest.ExpiryYear == DateTime.Today.Year)
            Assert.Contains(validationResponse.Errors, x => x.PropertyName == "ExpiryMonth/ExpiryYear");
        else
            Assert.Contains(validationResponse.Errors, x => x.PropertyName == "ExpiryYear");

        // Check that the bank was not called
        await _acquiringBankServiceMock.DidNotReceive().ProcessPaymentAsync(Arg.Any<BankPaymentRequest>());
    }

    [Fact(DisplayName = "Payment processing fails when bank throws error")]
    public async Task PostPaymentAsync_FailsWhenBankThrowsError()
    {
        // Arrange
        var paymentRequest = BuildValidPaymentRequest();

        var bankRequest = new BankPaymentRequest(
            paymentRequest.CardNumber, paymentRequest.ExpiryMonth, paymentRequest.ExpiryYear,
            paymentRequest.Currency, paymentRequest.Amount, paymentRequest.Cvv);

        var dummyBankResponse = new BankPaymentResponse { AuthorizationCode = "", Authorized = false };

        _acquiringBankServiceMock.ProcessPaymentAsync(bankRequest)
            .ThrowsAsyncForAnyArgs(new Exception("TEST"));


        // Act
        var response = await _apiClient.PostAsJsonAsync($"/api/Payments", paymentRequest);


        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    #endregion
}