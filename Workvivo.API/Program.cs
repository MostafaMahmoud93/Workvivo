using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;
using Serilog;
using Workvivo.API.Middleware;
using Workvivo.API.Options;
using Workvivo.Application;
using Workvivo.Infrastructure;

// A bootstrap logger, so a failure during configuration (a missing connection string,
// an invalid signing key) is written somewhere instead of vanishing. Replaced by the
// configured logger once the host is built.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.AddWorkvivoSerilog();

    // Do not advertise the server software. Free, and it removes one hint about which
    // known vulnerabilities are worth trying.
    builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

    #region Configuration and options

    // Options are validated at startup rather than on first use, so a bad deployment
    // fails immediately and visibly instead of at 3am on the first login attempt.
    builder.Services.AddOptions<JwtOptions>()
        .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
        .ValidateDataAnnotations()
        .ValidateOnStart();

    builder.Services.Configure<CorsOptions>(builder.Configuration.GetSection(CorsOptions.SectionName));

    // MailSettings and UploadPath keep their existing singleton registration so the
    // template's MailService and FileManager keep working unchanged.
    var mailSettings = builder.Configuration.GetSection("MailSettings").Get<MailSettings>() ?? new MailSettings();
    var uploadPath = builder.Configuration.GetSection("UploadPath").Get<UploadPath>() ?? new UploadPath();
    builder.Services.AddSingleton(mailSettings);
    builder.Services.AddSingleton(uploadPath);

    #endregion

    #region Services

    builder.Services.AddControllers();
    builder.Services.AddMemoryCache();
    builder.Services.AddHttpClient();
    builder.Services.AddSignalR();

    builder.Services.AddWorkvivoLocalization();
    builder.Services.AddWorkvivoCors(builder.Configuration);
    builder.Services.AddWorkvivoRateLimiting();
    builder.Services.AddWorkvivoSwagger();

    // A real ceiling instead of long.MaxValue. The per-category limits in
    // Storage:Validation are the ones that matter, but a transport-level cap stops a
    // multi-gigabyte body being buffered before any of them get a chance to run.
    builder.Services.Configure<FormOptions>(options =>
    {
        options.MultipartBodyLengthLimit = 512L * 1024 * 1024;
        options.ValueLengthLimit = 4 * 1024 * 1024;
        options.MultipartHeadersLengthLimit = 32 * 1024;
    });

    builder.Services.AddHealthChecks();

    ConfigureDatabase.ConfigureDatabases(builder.Services, builder.Configuration);
    ConfigureRepositoriesType.AddRepositoriesLayer(builder.Services);
    ConfigureAuthuntication.AddAuthuntication(builder.Services, builder.Configuration);
    builder.Services.AddAuthorization();

    builder.Services.AddInfrastructureLayer(builder.Configuration);
    builder.Services.AddApplicationLayer();

    // The template's own service registrations. Kept last so anything it registers
    // can override a default from the layers above.
    builder.Services.AddServiceLayer();

    #endregion

    var app = builder.Build();

    #region Pipeline

    // Correlation id, then the exception handler, then security headers - see
    // MiddlewareExtensions for why that order.
    app.UseWorkvivoDiagnostics();
    app.UseWorkvivoRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Workvivo API v1");
            options.DocumentTitle = "Workvivo API";
        });
    }
    else
    {
        // Only in production: HSTS on a development machine pins localhost to HTTPS in
        // the browser for months, which is a nuisance to undo.
        app.UseHsts();
    }

    app.UseHttpsRedirection();

    app.UseRequestLocalization(app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value);

    app.UseCors(CorsOptions.PolicyName);

    app.UseRateLimiter();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    // Two probes, because they answer different questions: "is the process alive"
    // (restart it if not) and "can it serve traffic" (route around it if not).
    app.MapHealthChecks("/health/live");
    app.MapHealthChecks("/health/ready");

    #endregion

    Log.Information(
        "Workvivo.API starting in {Environment}",
        app.Environment.EnvironmentName);

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Workvivo.API failed to start");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

/// <summary>Exposed so WebApplicationFactory can bootstrap the API in integration tests.</summary>
public partial class Program;
