using System.Security.Claims;
using Esatto.Outreach.Application.DTOs;
using Esatto.Outreach.Application.UseCases.CompanyInfo;
using Esatto.Outreach.Application.UseCases.ProjectCases;
namespace Esatto.Outreach.Api.Endpoints;

public static class CompanyInfoEndpoints
{
    public static void MapCompanyInfoEndpoints(this WebApplication app)
    {
        var companyInfo = app.MapGroup("/settings/company-info").WithTags("Company Info");

        // --- COMPANY INFO ---

        companyInfo.MapGet("/", async (GetCompanyInfo useCase, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            try
            {
                var info = await useCase.Handle(userId, ct);
                return info is null ? Results.NotFound() : Results.Ok(info);
            }
            catch (Exception ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: 500, title: "Failed to load company info");
            }
        }).RequireAuthorization();

        companyInfo.MapPut("/", async (CompanyInfoUpdateDto dto, UpdateCompanyInfo useCase, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            try
            {
                var info = await useCase.Handle(userId, dto, ct);
                return info is null ? Results.NotFound() : Results.Ok(info);
            }
            catch (UnauthorizedAccessException) { return Results.StatusCode(403); }
            catch (InvalidOperationException) { return Results.NotFound(); }
        }).RequireAuthorization();

        // --- PROJECT CASES ---

        companyInfo.MapGet("/cases", async (GetProjectCases useCase, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            var list = await useCase.Handle(userId, ct);
            return Results.Ok(list);
        }).RequireAuthorization();

        companyInfo.MapGet("/cases/{id:guid}", async (Guid id, GetProjectCase useCase, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            var pc = await useCase.Handle(id, userId, ct);
            return pc is null ? Results.NotFound() : Results.Ok(pc);
        }).RequireAuthorization();

        companyInfo.MapPost("/cases", async (ProjectCaseUpdateDto dto, CreateProjectCase useCase, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            try
            {
                var pc = await useCase.Handle(dto, userId, ct);
                return Results.Created($"/settings/company-info/cases/{pc.Id}", pc);
            }
            catch (UnauthorizedAccessException) { return Results.StatusCode(403); }
        }).RequireAuthorization();

        companyInfo.MapPut("/cases/{id:guid}", async (Guid id, ProjectCaseUpdateDto dto, UpdateProjectCase useCase, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            try
            {
                var pc = await useCase.Handle(id, dto, userId, ct);
                return pc is null ? Results.NotFound() : Results.Ok(pc);
            }
            catch (UnauthorizedAccessException) { return Results.StatusCode(403); }
        }).RequireAuthorization();

        companyInfo.MapDelete("/cases/{id:guid}", async (Guid id, DeleteProjectCase useCase, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            try
            {
                var deleted = await useCase.Handle(id, userId, ct);
                return deleted ? Results.NoContent() : Results.NotFound();
            }
            catch (UnauthorizedAccessException) { return Results.StatusCode(403); }
        }).RequireAuthorization();
    }
}
