using Infrastructure.Extensions;
using Infrastructure.SignalR.Hubs;
using Infrastructure.Persistence;
using Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Controllers + filtro global [Authorize]
builder.Services.AddControllers(options =>
{
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.Filters.Add(new AuthorizeFilter(policy));
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// JWT options
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("JWT configuration section not found");
if (string.IsNullOrEmpty(jwtOptions.SecretKey))
    throw new InvalidOperationException("JWT SecretKey not configured");

// AuthN/AuthZ con JWT (leer token desde cookie "jwt" y desde query para SignalR)
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                // SignalR via query string
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/game-hub"))
                {
                    context.Token = accessToken;
                    return Task.CompletedTask;
                }

                // Cookie "jwt"
                if (string.IsNullOrEmpty(context.Token))
                {
                    var cookie = context.Request.Cookies["jwt"];
                    if (!string.IsNullOrEmpty(cookie))
                        context.Token = cookie;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// Infraestructura (EF, repos, servicios, SignalR)
builder.Services.AddInfrastructure(builder.Configuration);

// CORS (HTTP + credenciales). IMPORTANTE: no usar AllowAnyOrigin con AllowCredentials.
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontDev", b =>
    {
        b.SetIsOriginAllowed(_ => true)
         .AllowAnyHeader()
         .AllowAnyMethod()
         .AllowCredentials();
    });
});


var app = builder.Build();

// Migraciones en Development
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<BattleTanksDbContext>();
    try
    {
        context.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}

// Swagger en Dev (Swagger UI debe quedar accesible aunque la API requiera auth global)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseCors("FrontDev");
app.UseAuthentication();
app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.MapHub<GameHub>("/game-hub");
});

app.Run();
