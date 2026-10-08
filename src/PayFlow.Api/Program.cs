using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using PayFlow.Api.Infrastructure;
using PayFlow.Application.Common.Interfaces;
using PayFlow.Application.Payroll.Interfaces;
using PayFlow.Application.Payroll.Services;
using PayFlow.Infrastructure.Data;
using PayFlow.Infrastructure.Services;
using Serilog;

// Enable Npgsql legacy timestamp behavior for seamless UTC/Unspecified DateTime mapping
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Add Controllers
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddHttpContextAccessor();

// Register Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
bool useInMemory = builder.Configuration.GetValue<bool>("USE_IN_MEMORY_DB", false);

builder.Services.AddDbContext<PayFlowDbContext>(options =>
{
    if (useInMemory)
    {
        // Fallback for isolated CI or offline testing
        options.UseInMemoryDatabase("PayFlowInMemoryDb");
    }
    else
    {
        options.UseNpgsql(connectionString, npgsql =>
        {
            npgsql.MigrationsAssembly("PayFlow.Infrastructure");
            npgsql.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorCodesToAdd: null);
        });
    }
});

// Register Domain and Application Services
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuditService, AuditService>();

builder.Services.AddScoped<ICompensationResolver, CompensationResolver>();
builder.Services.AddScoped<IAttendanceSummaryService, AttendanceSummaryService>();
builder.Services.AddScoped<ILeaveImpactCalculator, LeaveImpactCalculator>();
builder.Services.AddScoped<IOvertimeCalculator, OvertimeCalculator>();
builder.Services.AddScoped<ITaxCalculator, TaxCalculator>();
builder.Services.AddScoped<IPayrollCalculator, PayrollCalculator>();
builder.Services.AddScoped<IPayrollRunService, PayrollRunService>();

builder.Services.AddScoped<DataSeeder>();

// Configure JWT Authentication
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "EnterprisePayrollSuperSecretKeyPayFlow2026!SecureKeyMinimum256Bits";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "PayFlowHR";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "PayFlowApp";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    // Extract token from either Authorization header OR HttpOnly cookie
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            if (string.IsNullOrEmpty(context.Token))
            {
                if (context.Request.Cookies.TryGetValue("payflow_token", out var cookieToken))
                {
                    context.Token = cookieToken;
                }
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:5173", "http://localhost:3000", "http://127.0.0.1:5173" };
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Add Endpoints API Explorer & Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Pipeline configuration
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "PayFlow HR API v1");
    c.RoutePrefix = "swagger";
});

app.UseCors("CorsPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Database migration / creation and seeding at startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var db = services.GetRequiredService<PayFlowDbContext>();
        if (db.Database.IsRelational())
        {
            await db.Database.EnsureCreatedAsync();
        }
        else
        {
            await db.Database.EnsureCreatedAsync();
        }

        var seeder = services.GetRequiredService<DataSeeder>();
        await seeder.SeedAsync();
        logger.LogInformation("Database initialized and demo data seeded successfully.");
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Database initialization warning: {Message}", ex.Message);
    }
}

app.Run();

// Required for WebApplicationFactory in IntegrationTests
public partial class Program { }
