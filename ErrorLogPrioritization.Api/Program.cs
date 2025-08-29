using Hangfire;
using Hangfire.MemoryStorage;
using Serilog;
using Serilog.Enrichers.CorrelationId;
using ErrorLogPrioritization.Api.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Configure configuration options
builder.Services.Configure<CopilotStudioConfiguration>(
    builder.Configuration.GetSection(CopilotStudioConfiguration.SectionName));
builder.Services.Configure<FileStorageConfiguration>(
    builder.Configuration.GetSection(FileStorageConfiguration.SectionName));
builder.Services.Configure<SchedulingConfiguration>(
    builder.Configuration.GetSection(SchedulingConfiguration.SectionName));
builder.Services.Configure<PerformanceConfiguration>(
    builder.Configuration.GetSection(PerformanceConfiguration.SectionName));

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithCorrelationId()
    .Enrich.WithProperty("Application", "ErrorLogPrioritization.Api")
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.File("logs/log-.txt", 
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {CorrelationId} {Message:lj} {Properties:j}{NewLine}{Exception}")
    .CreateLogger();

// Use Serilog for logging
builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllers();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.SetIsOriginAllowed(origin => 
                Uri.TryCreate(origin, UriKind.Absolute, out var uri) && 
                uri.Host == "localhost")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
        else
        {
            policy.WithOrigins("http://localhost:4200")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
    });
});

// Configure Hangfire
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseMemoryStorage());

builder.Services.AddHangfireServer();

// Add HTTP client for Copilot Studio integration
builder.Services.AddHttpClient();

// Register services
builder.Services.AddScoped<ErrorLogPrioritization.Api.Utils.IJsonFileManager, ErrorLogPrioritization.Api.Utils.JsonFileManager>();
builder.Services.AddScoped<ErrorLogPrioritization.Api.Services.ILogService, ErrorLogPrioritization.Api.Services.LogService>();
builder.Services.AddScoped<ErrorLogPrioritization.Api.Services.IExcelExportService, ErrorLogPrioritization.Api.Services.ExcelExportService>();
builder.Services.AddScoped<ErrorLogPrioritization.Api.Services.IRetryService, ErrorLogPrioritization.Api.Services.RetryService>();
builder.Services.AddScoped<ErrorLogPrioritization.Api.Services.IHealthCheckService, ErrorLogPrioritization.Api.Services.HealthCheckService>();
builder.Services.AddScoped<ErrorLogPrioritization.Api.Services.IPerformanceMonitoringService, ErrorLogPrioritization.Api.Services.PerformanceMonitoringService>();
builder.Services.AddScoped<ErrorLogPrioritization.Api.Services.ICopilotStudioService>(provider =>
{
    var httpClient = provider.GetRequiredService<HttpClient>();
    var logger = provider.GetRequiredService<ILogger<ErrorLogPrioritization.Api.Services.CopilotStudioService>>();
    var configuration = provider.GetRequiredService<IConfiguration>();
    var jsonFileManager = provider.GetRequiredService<ErrorLogPrioritization.Api.Utils.IJsonFileManager>();
    return new ErrorLogPrioritization.Api.Services.CopilotStudioService(httpClient, logger, configuration, jsonFileManager);
});
builder.Services.AddScoped<ErrorLogPrioritization.Api.Services.IScheduledAnalysisService>(provider =>
{
    var jsonFileManager = provider.GetRequiredService<ErrorLogPrioritization.Api.Utils.IJsonFileManager>();
    var copilotStudioService = provider.GetRequiredService<ErrorLogPrioritization.Api.Services.ICopilotStudioService>();
    var logger = provider.GetRequiredService<ILogger<ErrorLogPrioritization.Api.Services.ScheduledAnalysisService>>();
    var backgroundJobClient = provider.GetRequiredService<IBackgroundJobClient>();
    return new ErrorLogPrioritization.Api.Services.ScheduledAnalysisService(jsonFileManager, copilotStudioService, logger, backgroundJobClient);
});

// Add health checks
builder.Services.AddHealthChecks()
    .AddCheck<ErrorLogPrioritization.Api.Services.FileSystemHealthCheck>("filesystem")
    .AddCheck<ErrorLogPrioritization.Api.Services.CopilotStudioHealthCheck>("copilot-studio");

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Error Log Prioritization API",
        Version = "v1",
        Description = "API for collecting, analyzing, and prioritizing error logs using AI-powered analysis",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Error Log Prioritization Team",
            Email = "support@errorlogprioritization.com"
        },
        License = new Microsoft.OpenApi.Models.OpenApiLicense
        {
            Name = "MIT License",
            Url = new Uri("https://opensource.org/licenses/MIT")
        }
    });

    // Include XML comments for better documentation
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    // Add security definition for future authentication
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    // Configure response examples
    c.EnableAnnotations();
});

var app = builder.Build();

// Validate configuration on startup
try
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    ConfigurationValidator.ValidateConfiguration(builder.Configuration, logger);
}
catch (Exception ex)
{
    Log.Fatal(ex, "Configuration validation failed during application startup");
    throw;
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Error Log Prioritization API v1");
        c.RoutePrefix = "swagger";
        c.DocumentTitle = "Error Log Prioritization API Documentation";
        c.DefaultModelsExpandDepth(2);
        c.DefaultModelRendering(Swashbuckle.AspNetCore.SwaggerUI.ModelRendering.Example);
        c.DisplayRequestDuration();
        c.EnableDeepLinking();
        c.EnableFilter();
        c.ShowExtensions();
    });
}

// Use correlation ID middleware
app.UseMiddleware<ErrorLogPrioritization.Api.Middleware.CorrelationIdMiddleware>();

// Use global exception handler
app.UseMiddleware<ErrorLogPrioritization.Api.Middleware.GlobalExceptionHandlerMiddleware>();

// Use CORS
app.UseCors("AllowAngularApp");

app.UseHttpsRedirection();

app.UseAuthorization();

// Configure Hangfire Dashboard
app.UseHangfireDashboard("/hangfire");

// Schedule daily analysis job at 1 AM
RecurringJob.AddOrUpdate<ErrorLogPrioritization.Api.Services.IScheduledAnalysisService>(
    "daily-log-analysis",
    service => service.ExecuteDailyAnalysisAsync(),
    "0 1 * * *", // Cron expression for 1 AM daily
    new RecurringJobOptions
    {
        TimeZone = TimeZoneInfo.Utc
    });

app.MapControllers();
app.Map("/", () => "Hello from API root!");

// Map health check endpoints
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready");
app.MapHealthChecks("/health/live");

app.Run();

// Ensure to flush and stop internal timers/threads before application-exit
Log.CloseAndFlush();

// Make Program class accessible for testing
public partial class Program { }
