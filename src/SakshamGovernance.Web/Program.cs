using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SakshamGovernance.Core.Entities;
using SakshamGovernance.Core.Enums;
using SakshamGovernance.Core.Interfaces;
using SakshamGovernance.Infrastructure.Data;
using SakshamGovernance.Infrastructure.Repositories;
using SakshamGovernance.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Flexible Database Configuration (In-Memory for zero-friction demo / SQL Server for production)
var useInMemory = builder.Configuration.GetValue<bool>("UseInMemoryDatabase", true);

if (useInMemory)
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseInMemoryDatabase("SakshamGovernanceDb"));
}
else
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? "Server=(localdb)\\mssqllocaldb;Database=SakshamGovernanceDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

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
        options.Password.RequireDigit = false;
        options.Password.RequiredLength = 6;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.User.RequireUniqueEmail = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// 3. Register Domain Repositories & Services
builder.Services.AddScoped<IComplianceRepository, ComplianceRepository>();
builder.Services.AddScoped<IGrievanceRepository, GrievanceRepository>();
builder.Services.AddScoped<IConductRepository, ConductRepository>();
builder.Services.AddScoped<ISlaEscalationService, SlaEscalationService>();

// 4. Register Continuous Autonomous Background SLA Daemon
builder.Services.AddHostedService<SlaEscalationBackgroundService>();

// 5. Web APIs & OpenAPI / Swagger
builder.Services.AddControllersWithViews();
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

// 6. CORS Policy for Web / Frontend Integrations
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowClientApp", policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .AllowAnyOrigin();
    });
});

var app = builder.Build();

// Ensure Database & Seed Realistic Initial Records for Hackathon Showcase
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

    context.Database.EnsureCreated();

    // Seed Sample Student User if not present
    var sampleStudent = await userManager.FindByIdAsync("STU-2026-0891");
    if (sampleStudent == null)
    {
        sampleStudent = new ApplicationUser
        {
            Id = "STU-2026-0891",
            UserName = "aarav.sharma@institution.edu.in",
            Email = "aarav.sharma@institution.edu.in",
            FullName = "Aarav Sharma",
            Role = UserRole.Student,
            Disability = DisabilityType.LocomotorDisability,
            CreatedAtUtc = DateTime.UtcNow
        };
        await userManager.CreateAsync(sampleStudent);
    }

    // Seed Sample Compliance Audits
    if (!context.ComplianceLogs.Any())
    {
        context.ComplianceLogs.AddRange(
            new ComplianceLog
            {
                MaterialId = "CSE-101-LECTURE-01",
                MaterialTitle = "Data Structures & Algorithms (Accessible PDF with Tactile Diagrams)",
                WcagScore = 92.50m,
                IsRpwdCompliant = true,
                AuditedAt = DateTime.UtcNow.AddDays(-1),
                AuditRemarks = "Meets WCAG 2.2 Level AA and Section 40 RPwD Act 2016 standard."
            },
            new ComplianceLog
            {
                MaterialId = "PHY-201-OPTICS-LAB",
                MaterialTitle = "Optics & Laser Experiment Manual (Scanned Untagged Images)",
                WcagScore = 58.00m,
                IsRpwdCompliant = false,
                AuditedAt = DateTime.UtcNow.AddDays(-3),
                AuditRemarks = "Failed: Missing image descriptions, contrast ratio below 4.5:1."
            }
        );
        await context.SaveChangesAsync();
    }

    // Seed Sample Grievances
    if (!context.GrievanceTickets.Any())
    {
        context.GrievanceTickets.AddRange(
            new GrievanceTicket
            {
                StudentId = sampleStudent.Id,
                IssueDescription = "Main Academic Block ramp slope exceeds 1:12 ratio; wheelchair access impaired.",
                Status = TicketStatus.Open,
                CreatedAt = DateTime.UtcNow.AddHours(-12),
                SLAExpiryDateTime = DateTime.UtcNow.AddHours(60) // 60h remaining in 72h SLA
            },
            new GrievanceTicket
            {
                StudentId = sampleStudent.Id,
                IssueDescription = "Semester Examination portal lacks screen-reader NVDA compatibility.",
                Status = TicketStatus.Escalated,
                CreatedAt = DateTime.UtcNow.AddHours(-85),
                SLAExpiryDateTime = DateTime.UtcNow.AddHours(-13), // Breached 13 hours ago
                EscalatedAt = DateTime.UtcNow.AddHours(-13)
            }
        );
        await context.SaveChangesAsync();
    }

    // Seed Sample Conduct Incidents (DPDP Act 2023 Compliant)
    if (!context.ConductLogs.Any())
    {
        context.ConductLogs.AddRange(
            new ConductLog
            {
                PseudonymizedUserHash = "8f3b14a67e9c5620df981e4ab612347890123456789abcdef0123456789abcde",
                IncidentType = IncidentType.AbleistSlur,
                SanitizedContextSnippet = "Excluded peer from group forum: '[REDACTED_DEROGATORY_COMMENT]'",
                AiConfidenceScore = 0.978m,
                FlaggedAtUtc = DateTime.UtcNow.AddDays(-2),
                DataRetentionExpiryUtc = DateTime.UtcNow.AddDays(88),
                IsReviewedByOmbudsman = false
            }
        );
        await context.SaveChangesAsync();
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

app.UseCors("AllowClientApp");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapControllerRoute(
    name: "mvc",
    pattern: "Home/{action=Index}/{id?}",
    defaults: new { controller = "Home" });

app.Run();
