using System.ComponentModel.DataAnnotations;
using System.Text.Json;

using Asp.Versioning;

using FluentValidation;

using Microsoft.AspNetCore.Mvc;

using PaymentGateway.Api.Mappings;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Application.Payments;

namespace PaymentGateway.Api.Controllers;

[ApiVersion(1)]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly IValidator<PostPaymentRequest> _validator;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        IPaymentService paymentService,
        IValidator<PostPaymentRequest> validator,
        ILogger<PaymentsController> logger)
    {
        _paymentService = paymentService;
        _validator = validator;
        _logger = logger;
    }

    /// <summary>Process a card payment through the acquiring bank.</summary>
    /// <response code="201">The bank made a decision. <c>status</c> is <c>Authorized</c> or <c>Declined</c>.</response>
    /// <response code="400">Rejected: the request is invalid and was not sent to the bank. No payment was created.</response>
    /// <param name="request">The payment to process.</param>
    /// <param name="idempotencyKey">
    /// Optional. Sending the same key with the same request again returns the original payment
    /// instead of charging the card twice. Up to 64 characters: letters, digits, <c>.</c>, <c>_</c>, <c>-</c>.
    /// </param>
    /// <param name="cancellationToken"></param>
    /// <response code="201">The bank made a decision. <c>status</c> is <c>Authorized</c> or <c>Declined</c>.
    /// Also returned, with the original payment, when an <c>Idempotency-Key</c> is repeated.</response>
    /// <response code="400">Rejected: the request is invalid and was not sent to the bank. No payment was created.</response>
    /// <response code="409">A request with the same <c>Idempotency-Key</c> is still being processed.</response>
    /// <response code="422">The <c>Idempotency-Key</c> was already used for a different request.</response>
    /// <response code="502">The bank could not give a decision. No payment was created; safe to retry.</response>
    [HttpPost]
    [ProducesResponseType(typeof(PostPaymentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<PostPaymentResponse>> PostPayment(
        PostPaymentRequest request,
        [FromHeader(Name = "Idempotency-Key")]
        [RegularExpression("^[A-Za-z0-9._-]{1,64}$", ErrorMessage = "Idempotency-Key must be 1-64 characters: letters, digits, '.', '_' or '-'.")]
        string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            // Only field names are logged: values may be card data.
            _logger.LogInformation(
                "Payment rejected: invalid {InvalidFields}",
                validation.Errors.Select(e => e.PropertyName).Distinct());
            // Keys use the same camelCase names as the JSON fields the merchant sent.
            foreach (var error in validation.Errors)
            {
                ModelState.AddModelError(JsonNamingPolicy.CamelCase.ConvertName(error.PropertyName), error.ErrorMessage);
            }

            return ValidationProblem(
                title: "Payment rejected",
                detail: "The request is invalid and was not sent to the bank.");
        }

        var result = await _paymentService.ProcessPaymentAsync(request.ToCommand(idempotencyKey), cancellationToken);
        if (!result.IsSuccess)
        {
            var (statusCode, title) = result.Error switch
            {
                var e when e == PaymentErrors.IdempotencyKeyInProgress => (StatusCodes.Status409Conflict, "Request in progress"),
                var e when e == PaymentErrors.IdempotencyKeyReused => (StatusCodes.Status422UnprocessableEntity, "Idempotency key reused"),
                _ => (StatusCodes.Status502BadGateway, "Bank unavailable"),
            };
            return Problem(statusCode: statusCode, title: title, detail: result.Error.Message);
        }

        return CreatedAtAction(nameof(GetPayment), new { id = result.Value.Id }, result.Value.ToPostPaymentResponse());
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(GetPaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public ActionResult<GetPaymentResponse> GetPayment(Guid id)
    {
        var payment = _paymentService.GetPayment(id);

        if (payment is null)
        {
            return NotFound();
        }

        return payment.ToGetPaymentResponse();
    }
}
