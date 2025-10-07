using Esatto.Outreach.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Esatto.Outreach.Infrastructure;

public class OutreachDbContext : DbContext
{
    public OutreachDbContext(DbContextOptions<OutreachDbContext> options) : base(options) { }

    public DbSet<Prospect> Prospects => Set<Prospect>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OutreachDbContext).Assembly);
    }
}
