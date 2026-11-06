using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Sadachar.Core.Entities;
using Sadachar.Core.Enums;
using Sadachar.Core.Interfaces;
using Sadachar.Infrastructure.Data;
using Sadachar.Infrastructure.Repositories;
using Sadachar.Infrastructure.Services;
using Sadachar.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Allow deployments and restricted local environments to choose a writable,
// persistent key-ring location for authentication cookies and antiforgery tokens.
var dataProtectionKeysDirectory = builder.Configuration["DataProtection:KeysDirectory"];
if (!string.IsNullOrWhiteSpace(dataProtectionKeysDirectory))
{
    Directory.CreateDirectory(dataProtectionKeysDirectory);
    builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysDirectory));
}

// 1. Flexible Database Configuration (In-Memory for zero-friction demo / SQL Server for production)
var useInMemory = builder.Configuration.GetValue<bool>("UseInMemoryDatabase", true);

if (useInMemory)
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseInMemoryDatabase("SadacharDb"));
}
else
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? "Server=(localdb)\\mssqllocaldb;Database=SadacharDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(connectionString, sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
        }));
}

// 2. ASP.NET Core Identity with Custom User & Roles
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.User.RequireUniqueEmail = true;
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();
builder.Services.Configure<Microsoft.AspNetCore.Identity.SecurityStampValidatorOptions>(options => options.ValidationInterval = TimeSpan.Zero);

builder.Services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, ApplicationUserClaimsPrincipalFactory>();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }

        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        }

        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
});

// 3. Register Domain Repositories & Services
builder.Services.AddScoped<IComplianceRepository, ComplianceRepository>();
builder.Services.AddScoped<IGrievanceRepository, GrievanceRepository>();
builder.Services.AddScoped<IConductRepository, ConductRepository>();
builder.Services.AddScoped<ISlaEscalationService, SlaEscalationService>();

// 4. Register Continuous Autonomous Background SLA Daemon
builder.Services.AddHostedService<SlaEscalationBackgroundService>();

// 5. Web APIs & OpenAPI / Swagger
builder.Services.AddControllersWithViews(options => options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute()));
builder.Services.AddAntiforgery(options => options.HeaderName = "RequestVerificationToken");
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Sadachar API",
        Version = "v1",
        Description = "Education Without Barriers — Enterprise Digital Ecosystem for Specially-Abled Students (RPwD Act 2016, WCAG 2.2, 72-Hour Grievance SLA, DPDP Act 2023)."
    });
});

var app = builder.Build();

// Apply database schema. LMS content and learner activity are created through authenticated workflows.
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    if (useInMemory) await context.Database.EnsureCreatedAsync();
    else await context.Database.MigrateAsync();

    var bootstrapEmail = builder.Configuration["BootstrapAdmin:Email"];
    var bootstrapPassword = builder.Configuration["BootstrapAdmin:Password"];
    if (!string.IsNullOrWhiteSpace(bootstrapEmail) && !string.IsNullOrWhiteSpace(bootstrapPassword))
    {
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        if (await users.FindByEmailAsync(bootstrapEmail) is null)
        {
            var bootstrapAdmin = new ApplicationUser
            {
                UserName = bootstrapEmail.Trim(), Email = bootstrapEmail.Trim(),
                FullName = builder.Configuration["BootstrapAdmin:FullName"]?.Trim() ?? "Administrator",
                Role = UserRole.Admin, CreatedAtUtc = DateTime.UtcNow
            };
            var result = await users.CreateAsync(bootstrapAdmin, bootstrapPassword);
            if (!result.Succeeded) throw new InvalidOperationException($"Could not create the configured administrator: {string.Join("; ", result.Errors.Select(e => e.Description))}");
        }
    }
}

// Configure the HTTP Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Sadachar API v1");
    });
}

app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapControllerRoute(
    name: "mvc",
    pattern: "Home/{action=Index}/{id?}",
    defaults: new { controller = "Home" });

app.Run();
