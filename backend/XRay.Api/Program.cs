using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using XRay.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

const string FrontendCorsPolicy = "FrontendCors";

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddHttpClient();
builder.Services.AddScoped<XRay.Api.Services.ICurrentUserService, XRay.Api.Services.CurrentUserService>();
builder.Services.AddScoped<XRay.Api.Services.ProjectService>();
builder.Services.AddScoped<XRay.Api.Services.IngestionService>();
builder.Services.AddScoped<XRay.Api.Services.ChangeService>();
builder.Services.AddScoped<XRay.Api.Services.SecurityScanService>();
builder.Services.AddScoped<XRay.Api.Services.AnalysisService>();
builder.Services.AddScoped<XRay.Api.Services.ReportService>();
builder.Services.AddScoped<XRay.Api.Services.IntegrationService>();
builder.Services.AddScoped<XRay.Api.Services.AiExplanationService>();
builder.Services.AddScoped<XRay.Api.Services.NotificationService>();
builder.Services.AddScoped<XRay.Api.Services.SettingsService>();

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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
