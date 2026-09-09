using System.Text;
using GrainMarket.Api.Common;
using GrainMarket.Api.Middleware;
using GrainMarket.Application;
using GrainMarket.Application.Common.Interfaces;
using GrainMarket.Infrastructure;
using GrainMarket.Infrastructure.Logging;
using GrainMarket.Infrastructure.Persistence;
using GrainMarket.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Serilog;

Log.Logger = SerilogSetup.Configure(new LoggerConfiguration()).CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // preserveStaticLogger: true — this host gets its own logger without touching the static
    // Log.Logger singleton, so multiple in-process WebApplicationFactory hosts (integration tests)
    // don't collide over the same "already frozen" bootstrap logger.
    builder.Host.UseSerilog((context, services, configuration) =>
        SerilogSetup.Configure(configuration).ReadFrom.Configuration(context.Configuration), preserveStaticLogger: true);

    // --- Services -----------------------------------------------------------------------

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ICurrentUser, CurrentUser>();

    builder.Services.AddControllers(options =>
    {
        options.Filters.Add<ValidationActionFilter>();
    });
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "Grain Market Management API", Version = "v1" });
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });
        c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
        });
    });

    var jwtSection = builder.Configuration.GetSection(JwtSettings.SectionName);
    var jwtSecret = jwtSection["Secret"]
        ?? throw new InvalidOperationException("Jwt:Secret is not configured. Set it in appsettings.json or via user-secrets/environment variables.");

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    }).AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });
    builder.Services.AddAuthorization();

    // CORS: the MAUI Blazor Hybrid client talks to this API over http(s)://localhost:<port> today
    // and over the LAN server's address once moved off the single till PC — both are covered by
    // configuration-driven allowed origins rather than AllowAnyOrigin.
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("ClientPolicy", policy =>
        {
            if (allowedOrigins.Length > 0)
            {
                policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
            }
        });
    });

    var app = builder.Build();

    // --- Migrate + seed on startup -------------------------------------------------------
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // The InMemory provider used by integration tests isn't relational and has no migrations;
        // EnsureCreated is the test-only equivalent. Production always runs on Postgres (relational).
        if (db.Database.IsRelational())
        {
            await db.Database.MigrateAsync();
        }
        else
        {
            await db.Database.EnsureCreatedAsync();
        }

        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        await SeedData.SeedAsync(db, passwordHasher);
    }

    // --- Pipeline --------------------------------------------------------------------------

    app.UseMiddleware<ExceptionHandlingMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseSerilogRequestLogging();

    app.UseHttpsRedirection();

    app.UseCors("ClientPolicy");

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "GrainMarket.Api terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

/// <summary>Exposed for WebApplicationFactory-based integration tests.</summary>
public partial class Program { }
