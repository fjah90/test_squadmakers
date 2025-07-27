using JokesApi.Controllers;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace JokesApi.Tests;

public class MathControllerTests
{
    private readonly MatematicasController _controller = new();

    [Fact]
    public void Lcm_ReturnsExpectedValue()
    {
        var result = _controller.Lcm("3,4") as OkObjectResult;
        Assert.NotNull(result);
        dynamic obj = result!.Value!;
        Assert.Equal(12, (long)obj.lcm);
    }

    [Fact]
    public void NextNumber_ReturnsPlusOne()
    {
        var result = _controller.NextNumber(7) as OkObjectResult;
        dynamic obj = result!.Value!;
        Assert.Equal(8, (int)obj.result);
    }
} 