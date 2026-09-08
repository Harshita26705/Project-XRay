using CgOne.Demo.Api.Models;
using CgOne.Demo.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CgOne.Demo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PaymentRequest request)
    {
        var result = await _paymentService.ProcessAsync(request);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public IActionResult GetById(int id)
    {
        return Ok(new PaymentResult { Accepted = true, TransactionId = id });
    }
}
