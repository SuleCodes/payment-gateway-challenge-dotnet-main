using Microsoft.AspNetCore.Mvc;

using PaymentGateway.Api.Mappings;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Application.Payments;

namespace PaymentGateway.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
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
