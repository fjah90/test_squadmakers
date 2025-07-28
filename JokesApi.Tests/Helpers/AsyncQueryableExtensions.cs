using System.Collections.Generic;
using System.Linq;
namespace JokesApi.Tests;

public static class AsyncQueryableExtensions
{
    public static IQueryable<T> AsAsyncQueryable<T>(this IEnumerable<T> source)
    {
        return new ChistesFilterTests.TestAsyncEnumerable<T>(source);
    }
} 