using JokesApi.Application.Ports;
using JokesApi.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace JokesApi.Application.UseCases;

public class GetCombinedJoke
{
    private readonly IChuckClient _chuck;
    private readonly IDadClient _dad;
    private readonly IUnitOfWork _uow;

    public GetCombinedJoke(IChuckClient chuck, IDadClient dad, IUnitOfWork uow)
    {
        _chuck = chuck;
        _dad = dad;
        _uow = uow;
    }

    public async Task<string> ExecuteAsync(CancellationToken ct = default)
    {
        var chuckTask = _chuck.GetRandomJokeAsync(ct);
        var dadTask = _dad.GetRandomJokeAsync(ct);
        var localList = await _uow.Jokes.Query.AsNoTracking().Select(j=>j.Text).ToListAsync(ct);

        await Task.WhenAll(chuckTask, dadTask);
        var chuck = chuckTask.Result;
        var dad = dadTask.Result;
        var local = localList.Count==0?null:localList[Random.Shared.Next(localList.Count)];

        var pieces = new List<string>();
        if (!string.IsNullOrWhiteSpace(chuck)) pieces.Add(chuck.Split('.')[0].Trim());
        if (!string.IsNullOrWhiteSpace(dad)) pieces.Add(dad.Split('.')[0].Trim());
        if (!string.IsNullOrWhiteSpace(local)) pieces.Add(local.Split('.')[0].Trim());

        if (pieces.Count==0)
            throw new InvalidOperationException("No jokes available");

        return string.Join(", ", pieces)+".";
    }
} 