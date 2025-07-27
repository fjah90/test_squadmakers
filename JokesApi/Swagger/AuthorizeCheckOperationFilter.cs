using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace JokesApi.Swagger;

/// <summary>
/// Adds the "Bearer" security requirement to endpoints decorated with <see cref="AuthorizeAttribute"/>,
/// so Swagger UI shows the lock icon.
/// </summary>
public class AuthorizeCheckOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var hasAuth = context.MethodInfo.DeclaringType?.GetCustomAttributes(true).OfType<AuthorizeAttribute>().Any() == true ||
                      context.MethodInfo.GetCustomAttributes(true).OfType<AuthorizeAttribute>().Any();

        if (!hasAuth) return;

        operation.Security ??= new List<OpenApiSecurityRequirement>();
        var scheme = new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } };
        operation.Security.Add(new OpenApiSecurityRequirement { [scheme] = new List<string>() });
    }
} 