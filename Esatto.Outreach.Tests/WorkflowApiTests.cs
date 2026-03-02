using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Esatto.Outreach.Application.DTOs;
using Esatto.Outreach.Domain.Entities;
using Esatto.Outreach.Domain.Enums;
using Esatto.Outreach.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Esatto.Outreach.Tests;

public record ActivateWorkflowRequest(string TimeZoneId = "UTC");

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    // Need a constructor compatible with .NET 8/9
    // public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock) ... IS OBSOLETE in 8.0?
    // In .NET 8+, use (IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    
    [Obsolete] 
    public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
        : base(options, logger, encoder, clock)
    {
    }

    public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[] 
        { 
            new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
            new Claim(ClaimTypes.Name, "Test User")
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

public class WorkflowApiTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _dbName = $"test_{Guid.NewGuid()}.db";

    public WorkflowApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            // Force Sqlite via Configuration
            builder.UseSetting("Database:Provider", "Sqlite");
            builder.UseSetting("ConnectionStrings:Default", $"Data Source={_dbName}");

            builder.ConfigureTestServices(services =>
            {
                // Remove existing DbContextOptions to ensure we don't have lingering config (optional but safe)
                // Actually, if we rely on AddInfrastructure, we shouldn't remove.
                // But AddInfrastructure runs in Program. Main Program uses config. 
                // We changed config. So AddInfrastructure uses Sqlite.
                
                // Mock Auth
                services.AddAuthentication(options => 
                {
                    options.DefaultAuthenticateScheme = "Test";
                    options.DefaultChallengeScheme = "Test";
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });

                // Mock AI Client
                services.AddScoped<Esatto.Outreach.Application.Abstractions.IGenerativeAIClient, FakeAIClient>();
                
                // Mock External Action Clients
                services.AddScoped<Esatto.Outreach.Application.Abstractions.IEmailSender, Esatto.Outreach.Infrastructure.Services.MockEmailSender>();
                services.AddScoped<Esatto.Outreach.Application.Abstractions.ILinkedInActionsClient, Esatto.Outreach.Infrastructure.Services.MockLinkedInClient>();
                
                // Mock Email Factory
                services.AddScoped<Esatto.Outreach.Application.Abstractions.IOutreachGeneratorFactory, FakeOutreachGeneratorFactory>();
            });
        });
    }

    private class FakeAIClient : Esatto.Outreach.Application.Abstractions.IGenerativeAIClient
    {
        public Task<string> GenerateTextAsync(string userInput, string? systemPrompt = null, bool useWebSearch = false, double temperature = 0.3, int maxOutputTokens = 1500, CancellationToken ct = default)
        {
            return Task.FromResult("SUBJECT: Test Subject\nBODY: Test Body");
        }
    }

    private class FakeOutreachGenerator : Esatto.Outreach.Application.Abstractions.IOutreachGenerator
    {
        public Task<CustomOutreachDraftDto> GenerateAsync(OutreachGenerationContext context, CancellationToken ct = default)
        {
            return Task.FromResult(new CustomOutreachDraftDto("New Subject", "New Body", "<p>New Body</p>", context.Channel));
        }
    }

    private class FakeOutreachGeneratorFactory : Esatto.Outreach.Application.Abstractions.IOutreachGeneratorFactory
    {
        public Esatto.Outreach.Application.Abstractions.IOutreachGenerator GetGenerator() => new FakeOutreachGenerator();
        public Esatto.Outreach.Application.Abstractions.IOutreachGenerator GetGenerator(string type) => new FakeOutreachGenerator();
    }

    [Fact]
    public async Task CreateWorkflow_ReturnsInstanceDto_WithoutCycles()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OutreachDbContext>();
            db.Database.EnsureCreated(); // Ensure In-Memory DB is ready

            var user = new ApplicationUser { Id = "test-user-id", UserName = "testuser", Email = "test@test.com" };
            if (!db.Users.Any(u => u.Id == user.Id)) db.Users.Add(user);
            
            var prospect = Prospect.CreateManual("Test Prospect", user.Id);
            prospect.AddContactPerson("Contact 1", "CEO", "c1@test.com");
            var contact = prospect.ContactPersons.Last();
            prospect.SetActiveContact(contact.Id);
            db.Prospects.Add(prospect);
            
            var generalPrompt = OutreachPrompt.Create(user.Id, "General info...", PromptType.General, true);
            var prompt = OutreachPrompt.Create(user.Id, "Refine draft...", PromptType.Email, true);
            db.Set<OutreachPrompt>().AddRange(generalPrompt, prompt);

            var intelligence = EntityIntelligence.Create(prospect.Id, "Simulated Context", null);
            db.Set<EntityIntelligence>().Add(intelligence);
            prospect.LinkEntityIntelligence(intelligence.Id);
            
            var template = WorkflowTemplate.Create("Test Template", "Desc");
            template.SetDefault(true);
            template.AddStep(WorkflowStepType.Email, 1, new TimeSpan(9, 0, 0), ContentGenerationStrategy.WebSearch); // Day 1, 9:00
            db.WorkflowTemplates.Add(template);

            await db.SaveChangesAsync();

            // Act
            // POST /prospects/{prospectId}/workflows with body: templateId
            var response = await client.PostAsJsonAsync($"/prospects/{prospect.Id}/workflows", template.Id);

            // Assert
            var content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                Assert.Fail($"Request failed with {response.StatusCode}: {content}");
            }
            response.EnsureSuccessStatusCode(); // 201 Created
            // Verify NO cycle error
            Assert.DoesNotContain("object cycle", content);
            
            // Verify structure
            var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
            
            var dto = await response.Content.ReadFromJsonAsync<WorkflowInstanceDto>(options);
            Assert.NotNull(dto);
            Assert.Equal(prospect.Id, dto.ProspectId);
            Assert.Single(dto.Steps);
        }
    }

    [Fact]
    public async Task CreateWorkflow_Fails_IfNoActiveContact()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OutreachDbContext>();
            db.Database.EnsureCreated();

            var user = new ApplicationUser { Id = "test-user-id-2", UserName = "testuser2", Email = "test2@test.com" };
            if (!db.Users.Any(u => u.Id == user.Id)) db.Users.Add(user);
            
            var prospect = Prospect.CreateManual("Test Prospect 2", user.Id);
            // No contacts added, or at least no active contact
            db.Prospects.Add(prospect);
            
            // Still need prompt and intelligence or it fails earlier?
            // ContextBuilder checks:
            // 2. Active Prompt
            // 3. Soft Data (Intelligence)
            // 4. Active Contact
            
            var prompt = OutreachPrompt.Create(user.Id, "Refine draft...", PromptType.Email, true);
            db.Set<OutreachPrompt>().Add(prompt);

            var intelligence = EntityIntelligence.Create(prospect.Id, "Context", null);
            db.Set<EntityIntelligence>().Add(intelligence);
            prospect.LinkEntityIntelligence(intelligence.Id);
            
            var template = WorkflowTemplate.Create("Test Template 2", "Desc");
            template.SetDefault(true);
            template.AddStep(WorkflowStepType.Email, 1, new TimeSpan(9, 0, 0), ContentGenerationStrategy.WebSearch);
            db.WorkflowTemplates.Add(template);

            await db.SaveChangesAsync();

            // Act
            // The service CreateInstanceFromTemplateAsync generates draft.
            // GenerateDraft calls ContextBuilder.
            // ContextBuilder treats No Active Contact as "Generic / Unknown" (lines 66-70).
            // So it DOES NOT FAIL in generation.
            
            // Wait, does Activate fail?
            // "Activation fails if no active contact".
            // The endpoint is `CreateInstance`. It creates a Draft.
            // Does it Activate?
            // Endpoint code: `service.CreateInstanceFromTemplateAsync` -> returns instance.
            // It puts instance in DRAFT state probably (Step Status Pending).
            // The service code `CreateInstanceFromTemplateAsync`: `instance.AddStep(...)` ... `GenerateDraft(...)` ... `AddInstanceAsync(instance)`.
            // Instance starts as Draft?
            
            // `WorkflowInstance.Create` sets status Draft.
            // `WorkflowInstanceService.ActivateAsync` checks active contact.
            
            // So `CreateInstance` should SUCCEED even without Active Contact.
            // But `Activate` should FAIL.
            
            // The user request was "Activation fails without active contact person".
            // So I should test Activate Endpoint.
            // POST /workflows/{id}/activate
            
            // 1. Create Instance (manually or via endpoint)
            // 2. Call Activate Endpoint
            
            // Let's create instance normally via endpoint (verified above that it succeeds with contact).
            // Without contact, Create should succeed.
            // Then Activate should fail.
            
            // Creating Instance via DB directly to save time.
            var instance = WorkflowInstance.Create(prospect.Id);
            instance.AddStep(WorkflowStepType.Email, 1, new TimeSpan(9, 0, 0), ContentGenerationStrategy.WebSearch);
            db.WorkflowInstances.Add(instance);
            await db.SaveChangesAsync();

            // Call Activate
            var response = await client.PostAsJsonAsync($"/workflow-instances/{instance.Id}/activate", new ActivateWorkflowRequest());
            
            // Assert
            Assert.False(response.IsSuccessStatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("active contact", content, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task CreateWorkflow_Fails_IfWorkflowExists()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OutreachDbContext>();
            db.Database.EnsureCreated();

            var user = new ApplicationUser { Id = "rec-user-id", UserName = "recuser", Email = "rec@test.com" };
            if (!db.Users.Any(u => u.Id == user.Id)) db.Users.Add(user);
            
            var prospect = Prospect.CreateManual("Rec Prospect", user.Id);
            prospect.AddContactPerson("Contact 1", "CEO", "c1@test.com"); // Valid contact
            db.Prospects.Add(prospect);
            
            // Seed Workflow Dependencies
            var prompt = OutreachPrompt.Create(user.Id, "Draft...", PromptType.Email, true);
            db.Set<OutreachPrompt>().Add(prompt);
            var intelligence = EntityIntelligence.Create(prospect.Id, "Context", null);
            db.Set<EntityIntelligence>().Add(intelligence);
            prospect.LinkEntityIntelligence(intelligence.Id);
            prospect.SetActiveContact(prospect.ContactPersons.First().Id);

            var template = WorkflowTemplate.Create("Rec Template", "Desc");
            template.SetDefault(true);
            template.AddStep(WorkflowStepType.Email, 1, new TimeSpan(9, 0, 0), ContentGenerationStrategy.WebSearch);
            db.WorkflowTemplates.Add(template);

            // Create First Instance (Existing)
            var instance = WorkflowInstance.Create(prospect.Id);
            instance.AddStep(WorkflowStepType.Email, 1, new TimeSpan(9, 0, 0), ContentGenerationStrategy.WebSearch);
            db.WorkflowInstances.Add(instance);

            await db.SaveChangesAsync();

            // Act
            // Attempt to create another one via API
            var response = await client.PostAsJsonAsync($"/prospects/{prospect.Id}/workflows", template.Id);

            // Assert
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }
    }

    [Fact]
    public async Task DeleteWorkflow_RemovesInstance()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OutreachDbContext>();
            db.Database.EnsureCreated();

            var user = new ApplicationUser { Id = "del-user-id", UserName = "deluser", Email = "del@test.com" };
            if (!db.Users.Any(u => u.Id == user.Id)) db.Users.Add(user);
            
            var prospect = Prospect.CreateManual("Del Prospect", user.Id);
            db.Prospects.Add(prospect);
            
            var instance = WorkflowInstance.Create(prospect.Id);
            db.WorkflowInstances.Add(instance);
            await db.SaveChangesAsync();

            // Act
            var response = await client.DeleteAsync($"/prospects/{prospect.Id}/workflow");

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            // Verify in DB
            var exists = await db.WorkflowInstances.AnyAsync(i => i.Id == instance.Id);
            Assert.False(exists, "Instance should be deleted");
        }
    }

    [Fact]
    public async Task EditWorkflow_AddUpdateDeleteSteps()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OutreachDbContext>();
            db.Database.EnsureCreated();

            var user = new ApplicationUser { Id = "test-user-id", UserName = "edituser", Email = "edit@test.com" };
            if (!db.Users.Any(u => u.Id == user.Id)) db.Users.Add(user);
            
            var prospect = Prospect.CreateManual("Edit Prospect", user.Id);
            prospect.AddContactPerson("Editor", "Editor", "e@test.com");
            prospect.SetActiveContact(prospect.ContactPersons.First().Id);
            db.Prospects.Add(prospect);
            
            // Dependencies for generation
            var prompt = OutreachPrompt.Create(user.Id, "Draft...", PromptType.Email, true);
            db.Set<OutreachPrompt>().Add(prompt);
            var intelligence = EntityIntelligence.Create(prospect.Id, "Context", null);
            db.Set<EntityIntelligence>().Add(intelligence);
            prospect.LinkEntityIntelligence(intelligence.Id);
            
            var instance = WorkflowInstance.Create(prospect.Id);
            db.WorkflowInstances.Add(instance);
            await db.SaveChangesAsync();

            // 1. ADD STEP
            var addResp = await client.PostAsJsonAsync($"/workflow-instances/{instance.Id}/steps", new 
            { 
                Type = WorkflowStepType.Email, 
                DayOffset = 0,
                TimeOfDay = "10:00",
                GenerationStrategy = ContentGenerationStrategy.WebSearch
            });
            if (!addResp.IsSuccessStatusCode)
            {
                var error = await addResp.Content.ReadAsStringAsync();
                throw new Exception($"Add Step Failed: {addResp.StatusCode} - {error}");
            }
            addResp.EnsureSuccessStatusCode();
            var jsonOptions = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            jsonOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
            
            var updatedInstance = await addResp.Content.ReadFromJsonAsync<WorkflowInstanceDto>(jsonOptions);
            Assert.NotNull(updatedInstance);
            Assert.Single(updatedInstance.Steps);
            var stepId = updatedInstance.Steps.First().Id;

            // 2. UPDATE CONTENT
            var patchResp = await client.PatchAsJsonAsync($"/workflow-steps/{stepId}/content", new 
            { 
                Subject = "New Subject", 
                Body = "New Body" 
            });
            patchResp.EnsureSuccessStatusCode();

            // Verify content update using GET (or db check) allows verifying side effect
            // Re-fetch instance
            var instancesResp = await client.GetFromJsonAsync<List<WorkflowInstanceDto>>($"/prospects/{prospect.Id}/workflows", jsonOptions);
            Assert.NotNull(instancesResp);
            var stepCheck = instancesResp.First().Steps.First();
            Assert.Equal("New Subject", stepCheck.EmailSubject);

            // 3. UPDATE CONFIG
            var putResp = await client.PutAsJsonAsync($"/workflow-steps/{stepId}", new 
            { 
                Type = WorkflowStepType.LinkedInMessage, 
                DayOffset = 1,
                TimeOfDay = "12:00",
                GenerationStrategy = ContentGenerationStrategy.WebSearch
            });
            putResp.EnsureSuccessStatusCode();
            
            instancesResp = await client.GetFromJsonAsync<List<WorkflowInstanceDto>>($"/prospects/{prospect.Id}/workflows", jsonOptions);
            Assert.NotNull(instancesResp);
            stepCheck = instancesResp.First().Steps.First();
            Assert.Equal(WorkflowStepType.LinkedInMessage, stepCheck.Type);
            Assert.Equal(1, stepCheck.DayOffset);
            // DTO returns string "hh:mm" or similar, need to check format logic in ToDto
            Assert.Equal("12:00", stepCheck.TimeOfDay);

            // 4. DELETE STEP
            var delResp = await client.DeleteAsync($"/workflow-instances/{instance.Id}/steps/{stepId}");
            delResp.EnsureSuccessStatusCode();

            instancesResp = await client.GetFromJsonAsync<List<WorkflowInstanceDto>>($"/prospects/{prospect.Id}/workflows", jsonOptions);
            Assert.NotNull(instancesResp);
            Assert.Empty(instancesResp.First().Steps);
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
