using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Runtime.InteropServices;
using Sadachar.Core.Entities;
using Sadachar.Core.Enums;
using Sadachar.Core.Interfaces;
using Sadachar.Infrastructure.Data;
using Sadachar.Infrastructure.Repositories;
using Sadachar.Infrastructure.Services;
using Sadachar.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Data Protection
var dataProtectionKeysDirectory = builder.Configuration["DataProtection:KeysDirectory"];
if (!string.IsNullOrWhiteSpace(dataProtectionKeysDirectory))
{
    Directory.CreateDirectory(dataProtectionKeysDirectory);
    builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysDirectory));
}

// 1. Database Configuration: SQL Server LocalDB with SQLite/InMemory fallback for cross-platform (macOS/Linux)
var useInMemory = builder.Configuration.GetValue<bool>("UseInMemoryDatabase", false);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Server=(localdb)\\mssqllocaldb;Database=AccessEdDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

if (useInMemory)
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseInMemoryDatabase("AccessEdDb"));
}
else if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && connectionString.Contains("(localdb)", StringComparison.OrdinalIgnoreCase))
{
    // LocalDB is Windows-only; use SQLite file on macOS/Linux for zero-friction local run
    var dbPath = Path.Combine(builder.Environment.ContentRootPath, "accessed.db");
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlite($"Data Source={dbPath}"));
}
else
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(connectionString, sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
        }));
}

// 2. ASP.NET Core Identity with Roles, Lockout & Security
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
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.Configure<SecurityStampValidatorOptions>(options => options.ValidationInterval = TimeSpan.Zero);

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireStudent", policy =>
        policy.RequireRole(AppRoles.Student, AppRoles.Administrator, "Admin"));
    options.AddPolicy("RequireTeacher", policy =>
        policy.RequireRole(AppRoles.Teacher, AppRoles.Administrator, "Educator", "Admin"));
    options.AddPolicy("RequireAdmin", policy =>
        policy.RequireRole(AppRoles.Administrator, "Admin"));
    options.AddPolicy("TeacherOrAdmin", policy =>
        policy.RequireRole(AppRoles.Teacher, AppRoles.Administrator, "Educator", "Admin"));
});

builder.Services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, ApplicationUserClaimsPrincipalFactory>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/Login";
    options.Cookie.Name = "AccessEd.Auth";
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;

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

// 3. Domain Repositories & Background Service
builder.Services.AddScoped<IComplianceRepository, ComplianceRepository>();
builder.Services.AddScoped<IGrievanceRepository, GrievanceRepository>();
builder.Services.AddScoped<IConductRepository, ConductRepository>();
builder.Services.AddScoped<ISlaEscalationService, SlaEscalationService>();
builder.Services.AddHostedService<SlaEscalationBackgroundService>();

// 4. Controllers, Views & Antiforgery
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
});

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken";
    options.Cookie.Name = "AccessEd.Antiforgery";
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "AccessEd Web API",
        Version = "v1",
        Description = "AccessEd — Education Ecosystem for Specially-Abled Students. RESTful APIs for Resources, Profiles, Tickets, Compliance Audits, and Reports Summary."
    });
    c.DocInclusionPredicate((docName, apiDesc) => 
        apiDesc.HttpMethod != null && (apiDesc.RelativePath?.StartsWith("api", StringComparison.OrdinalIgnoreCase) == true));
});

var app = builder.Build();

// Database auto-migrate on startup and seed mockup data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    if (context.Database.IsSqlServer())
    {
        try { await context.Database.MigrateAsync(); }
        catch { await context.Database.EnsureCreatedAsync(); }
    }
    else
    {
        await context.Database.EnsureCreatedAsync();
    }

    await ResourceCatalogSeeder.SeedAsync(context);
    await AccessEdSeeder.SeedAsync(scope.ServiceProvider);
}

// HTTP Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "AccessEd API v1");
    c.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllers();

app.Run();
