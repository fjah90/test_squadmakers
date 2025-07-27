using JokesApi.Controllers;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace JokesApi.Tests;

public class MathControllerTests
{
    private readonly MathController _controller = new(Microsoft.Extensions.Logging.Abstractions.NullLogger<MathController>.Instance);

    [Fact]
    public void Lcm_ReturnsExpectedValue()
    {
        var result = _controller.Lcm("3,4") as OkObjectResult;
        Assert.NotNull(result);
        var value = result!.Value!;
        var prop = value.GetType().GetProperty("lcm");
        Assert.NotNull(prop);
        Assert.Equal(12L, (long)prop!.GetValue(value)!);
    }

    [Fact]
    public void NextNumber_ReturnsPlusOne()
    {
        var result = _controller.NextNumber(7) as OkObjectResult;
        var val = result!.Value!;
        var p = val.GetType().GetProperty("result");
        Assert.NotNull(p);
        Assert.Equal(8, (int)p!.GetValue(val)!);
    }
} 