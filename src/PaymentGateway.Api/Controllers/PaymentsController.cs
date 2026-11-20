using System.Text.Json;

using FluentValidation;

using Microsoft.AspNetCore.Mvc;

using PaymentGateway.Api.Mappings;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Application.Payments;

namespace PaymentGateway.Api.Controllers;

[Route("api/[controller]")]
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
    /// <response code="502">The bank could not give a decision. No payment was created; safe to retry.</response>
    [HttpPost]
    [ProducesResponseType(typeof(PostPaymentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<PostPaymentResponse>> PostPayment(
        PostPaymentRequest request,
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

        var result = await _paymentService.ProcessPaymentAsync(request.ToCommand(), cancellationToken);
        if (!result.IsSuccess)
        {
            return Problem(
                statusCode: StatusCodes.Status502BadGateway,
                title: "Bank unavailable",
                detail: result.Error.Message);
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
