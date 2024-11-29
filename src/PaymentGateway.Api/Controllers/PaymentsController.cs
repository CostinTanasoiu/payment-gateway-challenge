using FluentValidation;

using Microsoft.AspNetCore.Mvc;

using PaymentGateway.Api.Helpers;
using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;
using PaymentGateway.Api.Validation;

namespace PaymentGateway.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PaymentsController : Controller
{
    private readonly ILogger<PaymentsController> _logger;
    private readonly IValidator<PostPaymentRequest> _requestValidator;
    private readonly IPaymentsRepository _paymentsRepository;
    private readonly IAcquiringBankService _acquiringBankService;

    public PaymentsController(
        ILogger<PaymentsController> logger,
        IValidator<PostPaymentRequest> requestValidator,
        IPaymentsRepository paymentsRepository, 
        IAcquiringBankService acquiringBankService)
    {
        _logger = logger;
        _requestValidator = requestValidator;
        _paymentsRepository = paymentsRepository;
        _acquiringBankService = acquiringBankService;
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PostPaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PostPaymentResponse?>> GetPaymentAsync(Guid id)
    {
        var payment = _paymentsRepository.Get(id);
        if (payment == null)
        {
            _logger.LogWarning($"Payment '{id}' not found.");
            return NotFound();
        }

        return Ok(payment);
    }

    /// <summary>
    /// Processes a new card payment.
    /// </summary>
    /// <param name="request">The payment request model.</param>
    /// <returns>201 Created if successful</returns>
    [HttpPost]
    [ProducesResponseType(typeof(PostPaymentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(PostPaymentFailedValidationResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<PostPaymentResponse>> PostPaymentAsync(PostPaymentRequest request)
    {
        var validationResult = await _requestValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
            return BadRequest(new PostPaymentFailedValidationResponse
            {
                Errors = ValidationHelper.ApiFriendlyValidationErrors(validationResult)
            });

        var bankRequest = new BankPaymentRequest(
            request.CardNumber, request.ExpiryMonth, request.ExpiryYear, request.Currency, request.Amount, request.Cvv);

        try
        {
            var bankResponse = await _acquiringBankService.ProcessPaymentAsync(bankRequest);

            var paymentResponse = new PostPaymentResponse
            {
                Id = Guid.NewGuid(),
                Amount = request.Amount,
                CardNumberLastFour = PaymentCardHelper.MaskCardNumber(request.CardNumber),
                Currency = request.Currency,
                ExpiryMonth = request.ExpiryMonth,
                ExpiryYear = request.ExpiryYear,
                Status = bankResponse.Authorized ? PaymentStatus.Authorized : PaymentStatus.Declined,
            };

            _paymentsRepository.Add(paymentResponse);

            return CreatedAtAction("GetPayment", new { id = paymentResponse.Id }, paymentResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process payment");
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}