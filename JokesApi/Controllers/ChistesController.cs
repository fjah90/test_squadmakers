using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;
using JokesApi.Data;
using JokesApi.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JokesApi.Domain.Repositories;

namespace JokesApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "user,admin")]
public class ChistesController : ControllerBase
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ChistesController> _logger;

    public ChistesController(IHttpClientFactory httpFactory, IUnitOfWork uow, ILogger<ChistesController> logger)
    {
        _httpFactory = httpFactory;
        _uow = uow;
        _logger = logger;
    }

    // External DTOs
    private record ChuckResponse(string value);
    private record DadResponse(string joke);

    [HttpGet("aleatorio")]
    public async Task<IActionResult> GetRandom([FromQuery] string? origen)
    {
        try
        {
            string jokeText;
            switch (origen?.ToLower())
            {
                case "chuck":
                    jokeText = await GetChuckJokeAsync();
                    break;
                case "dad":
                    jokeText = await GetDadJokeAsync();
                    break;
                case null:
                default:
                    var pickChuck = Random.Shared.Next(2) == 0;
                    jokeText = pickChuck ? await GetChuckJokeAsync() : await GetDadJokeAsync();
                    break;
            }
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
            var chuckTasks = Enumerable.Range(0, 5).Select(_ => GetChuckJokeAsync()).ToArray();
            var dadTasks = Enumerable.Range(0, 5).Select(_ => GetDadJokeAsync()).ToArray();

            await Task.WhenAll(chuckTasks.Concat(dadTasks));

            var result = new List<object>();
            for (int i = 0; i < 5; i++)
            {
                var chuck = chuckTasks[i].Result;
                var dad = dadTasks[i].Result;
                var combinado = $"{chuck} Also, {dad}";
                result.Add(new { id = i + 1, chuck, dad, combinado });
            }
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
            var chuck = await GetChuckJokeAsync();
            var dad = await GetDadJokeAsync();
            var localList = await _uow.Jokes.Query.AsNoTracking()
                .Select(j => j.Text)
                .ToListAsync();
            var local = localList.Count == 0 ? null : localList[Random.Shared.Next(localList.Count)];

            var pieces = new List<string>();
            if (!string.IsNullOrWhiteSpace(chuck))
                pieces.Add(chuck.Split('.')[0].Trim());
            if (!string.IsNullOrWhiteSpace(dad))
                pieces.Add(dad.Split('.')[0].Trim());
            if (!string.IsNullOrWhiteSpace(local))
                pieces.Add(local.Split('.')[0].Trim());

            if (pieces.Count == 0)
                return StatusCode(500, "No jokes available");

            var combined = string.Join(", ", pieces) + ".";
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

    [HttpGet("filtrar")]
    public async Task<IActionResult> Filter([FromQuery] int? minPalabras, [FromQuery] string? contiene, [FromQuery] Guid? autorId, [FromQuery] Guid? tematicaId)
    {
        var query = _uow.Jokes.Query.AsQueryable();

        if (minPalabras.HasValue)
            query = query.Where(j => j.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length >= minPalabras);
        if (!string.IsNullOrWhiteSpace(contiene))
            query = query.Where(j => j.Text.Contains(contiene));
        if (autorId.HasValue)
            query = query.Where(j => j.AuthorId == autorId);
        if (tematicaId.HasValue)
            query = query.Where(j => j.Themes.Any(t => t.Id == tematicaId));

        var result = await query.AsNoTracking().ToListAsync();
        return Ok(result);
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

    // Helper methods
    private async Task<string> GetChuckJokeAsync()
    {
        var client = _httpFactory.CreateClient("Chuck");
        var res = await client.GetFromJsonAsync<ChuckResponse>("/jokes/random");
        return res?.value ?? "";
    }

    private async Task<string> GetDadJokeAsync()
    {
        var client = _httpFactory.CreateClient("Dad");
        var res = await client.GetFromJsonAsync<DadResponse>(string.Empty);
        return res?.joke ?? "";
    }
} 