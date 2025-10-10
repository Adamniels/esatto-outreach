using Esatto.Outreach.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Esatto.Outreach.Infrastructure;

public class OutreachDbContext : DbContext
{
    // base(options) gör så det skickas vidare och jag kan registrera i dependency injection istället
    public OutreachDbContext(DbContextOptions<OutreachDbContext> options) : base(options) { }

    // Betyder bara: "jag har en tabell för prospect Entities"
    public DbSet<Prospect> Prospects => Set<Prospect>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // gör så jag använder det konifiguration som jag gjort i: ProspectConfiguration
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OutreachDbContext).Assembly);
    }
}
