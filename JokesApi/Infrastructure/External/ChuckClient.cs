using JokesApi.Application.Ports;
using System.Net.Http.Json;

namespace JokesApi.Infrastructure.External;

public class ChuckClient : IChuckClient
{
    private readonly IHttpClientFactory _factory;
    public ChuckClient(IHttpClientFactory factory)=>_factory=factory;
    private record ChuckResponse(string value);
    public async Task<string?> GetRandomJokeAsync(CancellationToken ct=default)
    {
        var client=_factory.CreateClient("Chuck");
        var res=await client.GetFromJsonAsync<ChuckResponse>("/jokes/random",ct);
        return res?.value;
    }
} 