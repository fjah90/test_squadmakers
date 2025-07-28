using JokesApi.Application.Ports;

namespace JokesApi.Application.UseCases;

public class GetPairedJokes
{
    private readonly IChuckClient _chuck;
    private readonly IDadClient _dad;

    public GetPairedJokes(IChuckClient chuck, IDadClient dad)
    {
        _chuck = chuck;
        _dad = dad;
    }

    public async Task<List<PairedJokeResult>> ExecuteAsync(int count = 5, CancellationToken ct = default)
    {
        var chuckTasks = Enumerable.Range(0, count).Select(_ => _chuck.GetRandomJokeAsync(ct)).ToArray();
        var dadTasks = Enumerable.Range(0, count).Select(_ => _dad.GetRandomJokeAsync(ct)).ToArray();

        await Task.WhenAll(chuckTasks.Concat(dadTasks));

        var result = new List<PairedJokeResult>();
        for (int i = 0; i < count; i++)
        {
            var chuck = chuckTasks[i].Result ?? string.Empty;
            var dad = dadTasks[i].Result ?? string.Empty;
            var combinado = $"{chuck} Also, {dad}";
            result.Add(new PairedJokeResult(i + 1, chuck, dad, combinado));
        }
        
        return result;
    }
    
    public record PairedJokeResult(int Id, string Chuck, string Dad, string Combinado);
} 