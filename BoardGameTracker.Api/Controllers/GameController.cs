using Microsoft.AspNetCore.Mvc;

namespace BoardGameTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GameController
{
    [HttpGet]
    public string Get()
    {
        return "Test Mikhaël";
    }
}