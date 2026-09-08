using CgOne.Demo.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CgOne.Demo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("{id}")]
    public IActionResult GetProfile(int id)
    {
        return Ok(_userService.GetProfile(id));
    }
}
