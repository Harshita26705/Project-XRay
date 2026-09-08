using CgOne.Demo.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CgOne.Demo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;

    public InventoryController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    [HttpGet("{sku}")]
    public IActionResult Availability(string sku)
    {
        return Ok(new { available = _inventoryService.IsAvailable(sku) });
    }
}
