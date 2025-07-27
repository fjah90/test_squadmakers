using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace JokesApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "user,admin")]
public class MatematicasController : ControllerBase
{
    private readonly ILogger<MatematicasController> _logger;

    public MatematicasController(ILogger<MatematicasController> logger)
    {
        _logger = logger;
    }

    [HttpGet("mcm")]
    public IActionResult Lcm([FromQuery] string numbers)
    {
        if (string.IsNullOrWhiteSpace(numbers))
            return BadRequest("numbers query param required");

        var parts = numbers.Split(',', StringSplitOptions.RemoveEmptyEntries);
        if (!parts.All(p => int.TryParse(p, out _)))
            return BadRequest("numbers must be integers");

        var ints = parts.Select(int.Parse).ToArray();
        if (ints.Length == 0)
            return BadRequest("Provide at least one number");

        long lcm = ints[0];
        for (int i = 1; i < ints.Length; i++)
        {
            lcm = LcmTwo(lcm, ints[i]);
        }
        return Ok(new { lcm });
    }

    [HttpGet("siguiente-numero")]
    public IActionResult NextNumber([FromQuery] int number)
    {
        return Ok(new { result = number + 1 });
    }

    private static long Gcd(long a, long b)
    {
        while (b != 0)
        {
            var temp = b;
            b = a % b;
            a = temp;
        }
        return a;
    }

    private static long LcmTwo(long a, long b) => a / Gcd(a, b) * b;
} 