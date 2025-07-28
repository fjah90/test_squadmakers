using JokesApi.Application.Ports;

namespace JokesApi.Application.UseCases;

public class GetRandomJoke
{
    private readonly IChuckClient _chuck;
    private readonly IDadClient _dad;

    public GetRandomJoke(IChuckClient chuck, IDadClient dad)
    {
        _chuck = chuck;
        _dad = dad;
    }

    public async Task<string> ExecuteAsync(string? origen, CancellationToken ct = default)
    {
        string jokeText;
        switch (origen?.ToLower())
        {
            case "chuck":
                jokeText = await _chuck.GetRandomJokeAsync(ct) ?? string.Empty;
                break;
            case "dad":
                jokeText = await _dad.GetRandomJokeAsync(ct) ?? string.Empty;
                break;
            case null:
            default:
                var pickChuck = Random.Shared.Next(2) == 0;
                jokeText = pickChuck 
                    ? await _chuck.GetRandomJokeAsync(ct) ?? string.Empty 
                    : await _dad.GetRandomJokeAsync(ct) ?? string.Empty;
                break;
        }
        
        return jokeText;
    }
} 