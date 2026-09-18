using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using XRay.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

const string FrontendCorsPolicy = "FrontendCors";

builder.Services.AddControllers();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("expensive", limiterOptions =>
    {
        limiterOptions.PermitLimit = 10;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueLimit = 0;
    });
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<XRay.Api.Services.ICurrentUserService, XRay.Api.Services.CurrentUserService>();
builder.Services.AddScoped<XRay.Api.Services.ProjectService>();
builder.Services.AddSingleton<XRay.Api.Services.Repositories.IRepositoryProvider, XRay.Api.Services.Repositories.LocalRepositoryProvider>();
builder.Services.AddSingleton<XRay.Api.Services.Repositories.IRepositoryProvider, XRay.Api.Services.Repositories.AzureDevOpsRepositoryProvider>();
builder.Services.AddSingleton<XRay.Api.Services.Repositories.RepositoryProviderFactory>();
builder.Services.AddScoped<XRay.Api.Services.IngestionService>();
builder.Services.AddScoped<XRay.Api.Services.ChangeService>();
builder.Services.AddScoped<XRay.Api.Services.SecurityScanService>();
builder.Services.AddScoped<XRay.Api.Services.AnalysisService>();
builder.Services.AddScoped<XRay.Api.Services.ReportService>();
builder.Services.AddScoped<XRay.Api.Services.IntegrationService>();
builder.Services.AddScoped<XRay.Api.Services.AzureDevOpsService>();
builder.Services.AddScoped<XRay.Api.Services.AiExplanationService>();
builder.Services.AddScoped<XRay.Api.Services.NotificationService>();
builder.Services.AddScoped<XRay.Api.Services.SettingsService>();
builder.Services.AddSingleton<XRay.Api.Services.BackgroundJobs.IBackgroundTaskQueue, XRay.Api.Services.BackgroundJobs.BackgroundTaskQueue>();
builder.Services.AddHostedService<XRay.Api.Services.BackgroundJobs.AnalysisJobWorker>();

// Defense-in-depth: config layering already prevents this outside Development, but fail fast
// rather than silently ignore it if that ever changes.
XRay.Api.Auth.DevAuthBypassGuard.EnsureNotEnabledOutsideDevelopment(
    builder.Environment.IsDevelopment(), builder.Configuration.GetValue<bool>("UseDevAuthBypass"));

var useDevAuthBypass = builder.Environment.IsDevelopment() && builder.Configuration.GetValue<bool>("UseDevAuthBypass");
if (useDevAuthBypass)
{
    builder.Services.AddAuthentication(XRay.Api.Auth.DevBypassAuthHandler.SchemeName)
        .AddScheme<XRay.Api.Auth.DevBypassAuthOptions, XRay.Api.Auth.DevBypassAuthHandler>(
            XRay.Api.Auth.DevBypassAuthHandler.SchemeName, _ => { });
}
else
{
    builder.Services
        .AddAuthentication(Microsoft.Identity.Web.Constants.Bearer)
        .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));
}
builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                             ?? Array.Empty<string>();
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
    });
});

var app = builder.Build();

if (useDevAuthBypass)
{
    app.Logger.LogWarning(
        "UseDevAuthBypass is enabled — every request is treated as an authenticated fixed local user. " +
        "This is only permitted in Development and must never be set in Production configuration.");
}

if (!app.Environment.IsDevelopment() &&
    (builder.Configuration.GetSection("Ingestion:AllowedLocalRepositoryRoots").Get<string[]>() ?? Array.Empty<string>()).Length == 0)
{
    app.Logger.LogWarning(
        "Ingestion:AllowedLocalRepositoryRoots is empty — any authenticated user can point project ingestion at " +
        "an arbitrary local filesystem path on this server. Configure an allow-list before exposing this outside local development.");
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DbSeeder.SeedAsync(db);
}

app.UseHttpsRedirection();

app.UseCors(FrontendCorsPolicy);

app.UseRouting();
app.UseAuthentication();

app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true &&
        context.Request.RouteValues.TryGetValue("projectId", out var projectValue) &&
        Guid.TryParse(projectValue?.ToString(), out var projectId))
    {
        var currentUser = context.RequestServices.GetRequiredService<XRay.Api.Services.ICurrentUserService>();
        var db = context.RequestServices.GetRequiredService<AppDbContext>();
        var user = await currentUser.GetOrProvisionUserAsync(context.User, context.RequestAborted);
        var organization = await currentUser.GetOrProvisionOrganizationAsync(user, context.RequestAborted);
        var allowed = await db.Projects.AnyAsync(project => project.ProjectId == projectId && project.OrganizationId == organization.OrganizationId && project.IsActive, context.RequestAborted);
        if (!allowed)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }
    }

    await next();
});

app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();

app.Run();
