using System.Text;
using JokesApi.Data;
using JokesApi.Services;
using JokesApi.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Net.Http.Headers;
using JokesApi.Notifications;
using AspNet.Security.OAuth.GitHub;
using Polly;
using Polly.Extensions.Http;
using JokesApi.Domain.Repositories;
using JokesApi.Infrastructure;
using JokesApi.Infrastructure.Repositories;
using JokesApi.Application.UseCases;
using AspNetCoreRateLimit;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Jokes API", 
        Version = "v1",
        Description = "API for managing jokes from multiple sources with user authentication",
        Contact = new OpenApiContact
        {
            Name = "API Support",
            Email = "support@jokesapi.com"
        },
        License = new OpenApiLicense
        {
            Name = "MIT License",
            Url = new Uri("https://opensource.org/licenses/MIT")
        }
    });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT like: Bearer {token}"
    };
    options.AddSecurityDefinition("Bearer", securityScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { securityScheme, new[] { "Bearer" } }
    });

    // include XML comments if present
    var xmlPath = System.IO.Path.Combine(AppContext.BaseDirectory, "JokesApi.xml");
    if (System.IO.File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);

    // Enable Swagger annotations
    options.EnableAnnotations();

    // add lock icon automatically to [Authorize] endpoints
    options.OperationFilter<JokesApi.Swagger.AuthorizeCheckOperationFilter>();
});
builder.Services.AddControllers();

// Configure IP Rate Limiting
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.Configure<IpRateLimitPolicies>(builder.Configuration.GetSection("IpRateLimitPolicies"));
builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
builder.Services.AddInMemoryRateLimiting();

// Configure login attempt rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    
    // Add policy for login endpoint
    options.AddPolicy("login", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(15)
            }));
});

// Configure database based on connection string
var connectionString = builder.Configuration.GetConnectionString("Default") ?? 
    throw new InvalidOperationException("Connection string 'Default' not found.");
if (connectionString.Contains("Host=")) 
{
    // PostgreSQL connection
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(connectionString));
}
else
{
    // SQLite connection (default)
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite(connectionString));
}

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
builder.Services.AddScoped<ITokenService, TokenService>();

builder.Services.Configure<NotificationSettings>(builder.Configuration.GetSection("Notification"));
builder.Services.AddSingleton<INotifier, EmailNotifier>();
builder.Services.AddSingleton<INotifier, SmsNotifier>();
builder.Services.AddScoped<AlertService>();

var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();
if (jwtSettings == null)
    throw new InvalidOperationException("JwtSettings configuration is missing");
var key = Encoding.UTF8.GetBytes(jwtSettings.Key);

// Removed duplicate AddAuthentication call; configuration is handled below.

var authSection = builder.Configuration.GetSection("Authentication");
var googleClientId = authSection["Google:ClientId"];
var googleClientSecret = authSection["Google:ClientSecret"];
var githubClientId = authSection["GitHub:ClientId"];
var githubClientSecret = authSection["GitHub:ClientSecret"];

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    })
    .AddGoogle("Google", options =>
    {
        options.ClientId = googleClientId!;
        options.ClientSecret = googleClientSecret!;
    })
    .AddGitHub(options =>
    {
        options.ClientId = githubClientId!;
        options.ClientSecret = githubClientSecret!;
        options.Scope.Add("user:email");
    });

builder.Services.AddAuthorization();

builder.Services.AddHttpClient("Chuck", c =>
{
    c.BaseAddress = new Uri("https://api.chucknorris.io/");
})
    .AddTransientHttpErrorPolicy(pb => pb.WaitAndRetryAsync(3, i => TimeSpan.FromSeconds(Math.Pow(2, i))))
    .AddTransientHttpErrorPolicy(pb => pb.CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)));

builder.Services.AddHttpClient("Dad", c =>
{
    c.BaseAddress = new Uri("https://icanhazdadjoke.com/");
    c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
})
    .AddTransientHttpErrorPolicy(pb => pb.WaitAndRetryAsync(3, i => TimeSpan.FromSeconds(Math.Pow(2, i))))
    .AddTransientHttpErrorPolicy(pb => pb.CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)));

builder.Services.AddScoped<IJokeRepository, JokeRepository>();
builder.Services.AddScoped<IThemeRepository, ThemeRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<JokesApi.Application.Ports.IChuckClient, JokesApi.Infrastructure.External.ChuckClient>();
builder.Services.AddScoped<JokesApi.Application.Ports.IDadClient, JokesApi.Infrastructure.External.DadClient>();

// Register Use Cases
builder.Services.AddScoped<GetCombinedJoke>();
builder.Services.AddScoped<GetRandomJoke>();
builder.Services.AddScoped<GetPairedJokes>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Jokes API v1");
        c.RoutePrefix = string.Empty; // Set Swagger UI at app root
        c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
        c.EnableFilter();
        c.DisplayRequestDuration();
    });
}

// Redirect to HTTPS only in non-development environments so local HTTP Swagger works.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Use IP rate limiting middleware
app.UseIpRateLimiting();

// Use rate limiter middleware
app.UseRateLimiter();

app.UseMiddleware<JokesApi.Middleware.ErrorHandlingMiddleware>();

// SEED DB
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    if (!db.Users.Any())
    {
        var admin = new JokesApi.Entities.User
        {
            Id = Guid.NewGuid(),
            Name = "Admin",
            Email = "admin@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
            Role = "admin"
        };
        db.Users.Add(admin);
        db.SaveChanges();
    }
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
