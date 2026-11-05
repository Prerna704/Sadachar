using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SakshamGovernance.Core.Entities;

namespace SakshamGovernance.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<ComplianceLog> ComplianceLogs => Set<ComplianceLog>();
    public DbSet<GrievanceTicket> GrievanceTickets => Set<GrievanceTicket>();
    public DbSet<ConductLog> ConductLogs => Set<ConductLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ComplianceLog Configurations
        builder.Entity<ComplianceLog>(entity =>
        {
            entity.HasIndex(e => e.MaterialId);
            entity.HasIndex(e => e.AuditedAt);
            entity.Property(e => e.WcagScore).HasPrecision(5, 2);
        });

        // GrievanceTicket Configurations & Indexes for fast SLA polling
        builder.Entity<GrievanceTicket>(entity =>
        {
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.SLAExpiryDateTime);
            entity.HasOne(e => e.Student)
                  .WithMany(u => u.GrievanceTickets)
                  .HasForeignKey(e => e.StudentId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ConductLog Configurations
        builder.Entity<ConductLog>(entity =>
        {
            entity.HasIndex(e => e.PseudonymizedUserHash);
            entity.HasIndex(e => e.DataRetentionExpiryUtc);
            entity.Property(e => e.AiConfidenceScore).HasPrecision(4, 3);
        });
    }
}

