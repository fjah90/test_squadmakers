using System.ComponentModel.DataAnnotations;
using JokesApi.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JokesApi.Domain.Repositories;
using JokesApi.Application.UseCases;
using Swashbuckle.AspNetCore.Annotations;

namespace JokesApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "user,admin")]
public class ChistesController : ControllerBase
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ChistesController> _logger;
    private readonly GetCombinedJoke _combinedUseCase;
    private readonly GetRandomJoke _randomUseCase;
    private readonly GetPairedJokes _pairedUseCase;

    public ChistesController(
        IUnitOfWork uow, 
        ILogger<ChistesController> logger, 
        GetCombinedJoke combinedUseCase,
        GetRandomJoke randomUseCase,
        GetPairedJokes pairedUseCase)
    {
        _uow = uow;
        _logger = logger;
        _combinedUseCase = combinedUseCase;
        _randomUseCase = randomUseCase;
        _pairedUseCase = pairedUseCase;
    }

    [HttpGet("aleatorio")]
    public async Task<IActionResult> GetRandom([FromQuery] string? origen)
    {
        try
        {
            var jokeText = await _randomUseCase.ExecuteAsync(origen);
            return Ok(new { joke = jokeText });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching random joke");
            return StatusCode(500, "Error fetching joke");
        }
    }

    [HttpGet("emparejados")]
    public async Task<IActionResult> GetPaired()
    {
        try
        {
            var result = await _pairedUseCase.ExecuteAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pairing jokes");
            return StatusCode(500, "Error pairing jokes");
        }
    }

    [HttpGet("combinado")]
    public async Task<IActionResult> GetCombined()
    {
        try
        {
            var combined = await _combinedUseCase.ExecuteAsync();
            return Ok(new { combined });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating combined joke");
            return StatusCode(500, "Error generating combined joke");
        }
    }

    // Local jokes
    public record CreateJokeRequest([Required, MinLength(1)] string Text, List<Guid>? ThemeIds);

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateJokeRequest req)
    {
        var userIdStr = User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(userIdStr, out var userId)) return Unauthorized();

        var joke = new Joke
        {
            Id = Guid.NewGuid(),
            Text = req.Text,
            AuthorId = userId,
            Source = "Local"
        };

        if (req.ThemeIds?.Any() == true)
        {
            var themes = await _uow.Themes.Query.Where(t => req.ThemeIds.Contains(t.Id)).ToListAsync();
            joke.Themes = themes;
        }

        await _uow.Jokes.AddAsync(joke);
        await _uow.SaveAsync();
        return CreatedAtAction(nameof(GetById), new { id = joke.Id }, joke);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var joke = await _uow.Jokes.Query.AsNoTracking().Include(j => j.Author).FirstOrDefaultAsync(j => j.Id == id);
        return joke is null ? NotFound() : Ok(joke);
    }

    /// <summary>
    /// Filtra chistes según varios criterios
    /// </summary>
    /// <param name="minPalabras">Número mínimo de palabras que debe tener el chiste</param>
    /// <param name="contiene">Texto que debe contener el chiste</param>
    /// <param name="autorId">ID del autor del chiste</param>
    /// <param name="tematicaId">ID de la temática del chiste</param>
    /// <returns>Lista de chistes que cumplen con los criterios de filtrado</returns>
    [HttpGet("filtrar")]
    [SwaggerOperation(
        Summary = "Filtra chistes según varios criterios",
        Description = "Permite filtrar chistes por número de palabras, contenido, autor y temática"
    )]
    [SwaggerResponse(200, "Lista de chistes filtrados", typeof(List<Joke>))]
    public async Task<IActionResult> Filter(
        [FromQuery] int? minPalabras, 
        [FromQuery] string? contiene, 
        [FromQuery] Guid? autorId, 
        [FromQuery] Guid? tematicaId)
    {
        try
        {
            // Iniciar con una consulta IQueryable para construir la consulta de manera eficiente
            var query = _uow.Jokes.Query
                .Include(j => j.Author)
                .Include(j => j.Themes)
                .AsQueryable();

            // Aplicar filtros solo si se proporcionan los parámetros
            if (minPalabras.HasValue && minPalabras.Value > 0)
            {
                _logger.LogInformation("Filtrando por mínimo de palabras: {minPalabras}", minPalabras.Value);
                query = query.Where(j => j.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length >= minPalabras.Value);
            }
            
            if (!string.IsNullOrWhiteSpace(contiene))
            {
                _logger.LogInformation("Filtrando por contenido: {contiene}", contiene);
                query = query.Where(j => j.Text.Contains(contiene));
            }
            
            if (autorId.HasValue)
            {
                _logger.LogInformation("Filtrando por autor: {autorId}", autorId.Value);
                query = query.Where(j => j.AuthorId == autorId.Value);
            }
            
            if (tematicaId.HasValue)
            {
                _logger.LogInformation("Filtrando por temática: {tematicaId}", tematicaId.Value);
                query = query.Where(j => j.Themes.Any(t => t.Id == tematicaId.Value));
            }

            // Ejecutar la consulta y devolver los resultados
            var result = await query
                .AsNoTracking()
                .Select(j => new {
                    j.Id,
                    j.Text,
                    j.Source,
                    Author = new { j.Author.Id, j.Author.Name },
                    Themes = j.Themes.Select(t => new { t.Id, t.Name })
                })
                .ToListAsync();
                
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al filtrar chistes");
            return StatusCode(500, "Error al filtrar chistes");
        }
    }

    public record UpdateJokeRequest([Required, MinLength(1)] string Text);

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateJokeRequest req)
    {
        var joke = await _uow.Jokes.Query.FirstOrDefaultAsync(j => j.Id == id);
        if (joke is null) return NotFound();

        var userIdStr = User.FindFirst("sub")?.Value;
        Guid.TryParse(userIdStr, out var userId);
        var isAdmin = User.IsInRole("admin");

        if (joke.AuthorId != userId && !isAdmin)
            return Forbid();

        joke.Text = req.Text;
        await _uow.SaveAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var joke = await _uow.Jokes.Query.FirstOrDefaultAsync(j => j.Id == id);
        if (joke is null) return NotFound();

        var userIdStr = User.FindFirst("sub")?.Value;
        Guid.TryParse(userIdStr, out var userId);
        var isAdmin = User.IsInRole("admin");

        if (joke.AuthorId != userId && !isAdmin)
            return Forbid();

        _uow.Jokes.Remove(joke);
        await _uow.SaveAsync();
        return NoContent();
    }
} 