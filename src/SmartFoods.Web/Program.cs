// src/SmartFoods.Web/Program.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using SmartFoods.Web.Components;
using SmartFoods.Web.Data;
using SmartFoods.Web.Models.Identity;
using SmartFoods.Web.Services.Interfaces;
using SmartFoods.Web.Services.Authentication;
using SmartFoods.Web.Services.Dashboard;
using SmartFoods.Web.Services.Infrastructure;
using Hangfire;
using Hangfire.PostgreSql;

var builder = WebApplication.CreateBuilder(args);

// Register standard email service
builder.Services.AddScoped<IEmailService, SmtpEmailService>();

// Configure Hangfire Background Processing to use your existing Connection String
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(c => c.UseNpgsqlConnection(builder.Configuration.GetConnectionString("DefaultConnection"))));

// Run the background server agent inside the web application instance
builder.Services.AddHangfireServer();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add Authentication/Authorization Core Utilities
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddScoped<IAuthService, AuthService>();

//Memory cache register
builder.Services.AddMemoryCache();

// Add DbContext
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"));
});

// Add Identity with explicit generic type configurations matching ApplicationDbContext
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/login";
    options.AccessDeniedPath = "/access-denied"; // Optional: For unauthorized roles
});

builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Dashboard services

builder.Services.AddScoped<IDashboardService, DashboardService>();

// Register Recipe Infrastructure Integrations using an isolated Factory descriptor
builder.Services.AddHttpClient();
builder.Services.AddScoped<IRecipeIntegrationService, SpoonacularIntegrationService>();

builder.Services.AddScoped<DashboardStateHub>();

// Register the notification abstraction layers as Scoped services
builder.Services.AddScoped<INotificationService, NotificationService>();



var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// Securely map Hangfire Dashboard UI panel (Accessible at http://localhost:5000/hangfire)
app.MapHangfireDashboard();

app.MapRazorComponents<SmartFoods.Web.Components.App>()
    .AddInteractiveServerRenderMode();

// Schedule the expiry tracking routine to run automatically every morning at 8:00 AM
RecurringJob.AddOrUpdate<PantryReminderJob>(
    "daily-expiry-reminders",
    job => job.SendDailyExpiryRemindersAsync(),
    Cron.Daily(8, 0));
    
app.Run();
