using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace JokesApi.Controllers;

/// <summary>
/// Provides simple math utilities.
/// </summary>
[ApiController]
[Authorize(Roles = "user,admin")]
[Route("api/matematicas")] // ruta oficial en español
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
    [SwaggerOperation(
        Summary = "Calcula el mínimo común múltiplo (MCM) de una lista de números",
        Description = "Recibe una lista de números separados por comas y devuelve su MCM"
    )]
    [SwaggerResponse(200, "MCM calculado correctamente", typeof(object))]
    [SwaggerResponse(400, "Parámetros incorrectos")]
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
    /// Devuelve el siguiente número entero (n + 1).
    /// </summary>
    [HttpGet("siguiente-numero")]
    [SwaggerOperation(
        Summary = "Devuelve el siguiente número entero",
        Description = "Incrementa en 1 el número proporcionado"
    )]
    [SwaggerResponse(200, "Siguiente número calculado correctamente", typeof(object))]
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
    
    /// <summary>
    /// Suma dos números enteros.
    /// </summary>
    /// <param name="a">Primer número</param>
    /// <param name="b">Segundo número</param>
    /// <returns>La suma de a y b</returns>
    [HttpGet("/api/math/add")]
    [SwaggerOperation(
        Summary = "Suma dos números enteros",
        Description = "Recibe dos números enteros y devuelve su suma"
    )]
    [SwaggerResponse(200, "Suma calculada correctamente", typeof(int))]
    public IActionResult Add([FromQuery] int a, [FromQuery] int b)
    {
        return Ok(a + b);
    }
    
    /// <summary>
    /// Resta dos números enteros.
    /// </summary>
    /// <param name="a">Primer número</param>
    /// <param name="b">Segundo número</param>
    /// <returns>La diferencia entre a y b</returns>
    [HttpGet("/api/math/subtract")]
    [SwaggerOperation(
        Summary = "Resta dos números enteros",
        Description = "Recibe dos números enteros y devuelve su diferencia"
    )]
    [SwaggerResponse(200, "Resta calculada correctamente", typeof(int))]
    public IActionResult Subtract([FromQuery] int a, [FromQuery] int b)
    {
        return Ok(a - b);
    }
    
    /// <summary>
    /// Multiplica dos números enteros.
    /// </summary>
    /// <param name="a">Primer número</param>
    /// <param name="b">Segundo número</param>
    /// <returns>El producto de a y b</returns>
    [HttpGet("/api/math/multiply")]
    [SwaggerOperation(
        Summary = "Multiplica dos números enteros",
        Description = "Recibe dos números enteros y devuelve su producto"
    )]
    [SwaggerResponse(200, "Multiplicación calculada correctamente", typeof(int))]
    public IActionResult Multiply([FromQuery] int a, [FromQuery] int b)
    {
        return Ok(a * b);
    }
} 