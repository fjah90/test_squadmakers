using JokesApi.Application.Ports;
using System.Net.Http.Json;

namespace JokesApi.Infrastructure.External;

public class DadClient : IDadClient
{
    private readonly IHttpClientFactory _factory;
    public DadClient(IHttpClientFactory factory)=>_factory=factory;
    private record DadResponse(string joke);
    public async Task<string?> GetRandomJokeAsync(CancellationToken ct=default)
    {
        var client=_factory.CreateClient("Dad");
        var res=await client.GetFromJsonAsync<DadResponse>(string.Empty,ct);
        return res?.joke;
    }
} 