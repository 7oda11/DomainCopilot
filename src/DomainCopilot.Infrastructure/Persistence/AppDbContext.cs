using DomainCopilot.Domain.Cases;
using DomainCopilot.Domain.Documents;
using DomainCopilot.Domain.Workflows;
using DomainCopilot.Application.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DomainCopilot.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public DbSet<ClinicalCase> ClinicalCases => Set<ClinicalCase>();
    public DbSet<SourceDocument> SourceDocuments => Set<SourceDocument>();
    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();
    public DbSet<CopilotRun> CopilotRuns => Set<CopilotRun>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
