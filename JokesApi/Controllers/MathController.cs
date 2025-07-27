using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JokesApi.Controllers;

/// <summary>
/// Provides simple math utilities.
/// </summary>
[ApiController]
[Authorize(Roles = "user,admin")]
[Route("api/math")] // new route
[Route("api/matematicas")] // legacy route for backward compatibility
public class MathController : ControllerBase
{
    private readonly ILogger<MathController> _logger;
    public MathController(ILogger<MathController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Calculates the least common multiple (LCM) of a list of integers.
    /// </summary>
    /// <param name="numbers">Comma-separated integers (e.g. 3,4,5).</param>
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

    /// <summary>
    /// Returns the next integer (n + 1).
    /// </summary>
    [HttpGet("next-number")]
    public IActionResult NextNumber([FromQuery] int number) => Ok(new { result = number + 1 });

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