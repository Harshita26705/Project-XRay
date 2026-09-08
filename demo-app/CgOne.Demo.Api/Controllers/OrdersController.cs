using CgOne.Demo.Api.Models;
using CgOne.Demo.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CgOne.Demo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpPost]
    public async Task<IActionResult> Place([FromBody] OrderRequest request)
    {
        var orderId = await _orderService.PlaceOrderAsync(request);
        return Ok(new { orderId });
    }
}
