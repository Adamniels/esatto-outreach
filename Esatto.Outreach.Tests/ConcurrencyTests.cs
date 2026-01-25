using System;
using System.Threading.Tasks;
using Esatto.Outreach.Domain.Entities;
using Esatto.Outreach.Domain.Enums;
using Esatto.Outreach.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Esatto.Outreach.Tests;

public class ConcurrencyTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _dbName = $"concurrency_{Guid.NewGuid()}.db";

    public ConcurrencyTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Database:Provider", "Sqlite");
            builder.UseSetting("ConnectionStrings:Default", $"Data Source={_dbName}");
        });
    }

    [Fact(Skip = "SQLite doesn't support RowVersion concurrency - requires PostgreSQL with trigger")]
    public async Task WorkflowStep_Concurrency_ThrowsException()
    {
        // 1. Setup Data
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OutreachDbContext>();
            db.Database.EnsureCreated();

            // Create User and Prospect
            var user = new ApplicationUser { Id = Guid.NewGuid().ToString(), UserName = "conc-user", Email = "conc@test.com" };
            db.Users.Add(user);
            var prospect = Prospect.CreateManual("Conc Prospect", user.Id);
            db.Prospects.Add(prospect);
            
            var instance = WorkflowInstance.Create(prospect.Id); 
            instance.AddStep(WorkflowStepType.Email, 0, TimeSpan.FromHours(9), ContentGenerationStrategy.WebSearch);
            db.WorkflowInstances.Add(instance);
            await db.SaveChangesAsync();
        }

        // 2. Simulate User A and User B loading the same entity
        using (var scopeA = _factory.Services.CreateScope())
        using (var scopeB = _factory.Services.CreateScope())
        {
            var dbA = scopeA.ServiceProvider.GetRequiredService<OutreachDbContext>();
            var dbB = scopeB.ServiceProvider.GetRequiredService<OutreachDbContext>();

            var stepA = await dbA.WorkflowSteps.FirstAsync();
            var stepB = await dbB.WorkflowSteps.FirstAsync();

            Assert.Equal(stepA.Id, stepB.Id);
            
            // 3. User A modifies and saves
            stepA.MarkExecuting();
            await dbA.SaveChangesAsync();

            // 4. User B tries to modify OLD version and save
            stepB.MarkExecuting(); // Or any change

            // Assert: Should throw DbUpdateConcurrencyException
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(async () => 
            {
                await dbB.SaveChangesAsync();
            });
        }
    }

    public void Dispose()
    {
        if (System.IO.File.Exists(_dbName))
        {
            try { System.IO.File.Delete(_dbName); } catch {}
        }
    }
}
