using System.Security.Claims;
using Esatto.Outreach.Api.Requests.CompanyInfo;
using Esatto.Outreach.Application.Features.Intelligence.GetCompanyInfo;
using Esatto.Outreach.Application.Features.Intelligence.UpdateCompanyInfo;
using Esatto.Outreach.Application.Features.ProjectCases.CreateProjectCase;
using Esatto.Outreach.Application.Features.ProjectCases.DeleteProjectCase;
using Esatto.Outreach.Application.Features.ProjectCases.GetProjectCase;
using Esatto.Outreach.Application.Features.ProjectCases.ListProjectCases;
using Esatto.Outreach.Application.Features.ProjectCases.UpdateProjectCase;

namespace Esatto.Outreach.Api.Endpoints;

public static class CompanyInfoEndpoints
{
    public static void MapCompanyInfoEndpoints(this WebApplication app)
    {
        var companyInfo = app.MapGroup("/settings/company-info").WithTags("Company Info");

        // --- COMPANY INFO ---

        companyInfo.MapGet("/", async (GetCompanyInfoQueryHandler handler, ClaimsPrincipal user, CancellationToken ct) =>
        {
            if (!user.TryGetUserId(out var userId)) return Results.Unauthorized();

            try
            {
                var info = await handler.Handle(new GetCompanyInfoQuery(), userId, ct);
                return info is null ? Results.NotFound() : Results.Ok(info);
            }
            catch (Exception ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: 500, title: "Failed to load company info");
            }
        }).RequireAuthorization();

        companyInfo.MapPut("/", async (UpdateCompanyInfoRequest req, UpdateCompanyInfoCommandHandler handler, ClaimsPrincipal user, CancellationToken ct) =>
        {
            if (!user.TryGetUserId(out var userId)) return Results.Unauthorized();

            try
            {
                var info = await handler.Handle(new UpdateCompanyInfoCommand(req.Name, req.Overview, req.ValueProposition), userId, ct);
                return info is null ? Results.NotFound() : Results.Ok(info);
            }
            catch (UnauthorizedAccessException) { return Results.StatusCode(403); }
            catch (InvalidOperationException) { return Results.NotFound(); }
        }).RequireAuthorization();

        // --- PROJECT CASES ---

        companyInfo.MapGet("/cases", async (ListProjectCasesQueryHandler handler, ClaimsPrincipal user, CancellationToken ct) =>
        {
            if (!user.TryGetUserId(out var userId)) return Results.Unauthorized();

            var list = await handler.Handle(new ListProjectCasesQuery(), userId, ct);
            return Results.Ok(list);
        }).RequireAuthorization();

        companyInfo.MapGet("/cases/{id:guid}", async (Guid id, GetProjectCaseQueryHandler handler, ClaimsPrincipal user, CancellationToken ct) =>
        {
            if (!user.TryGetUserId(out var userId)) return Results.Unauthorized();

            var pc = await handler.Handle(new GetProjectCaseQuery(id), userId, ct);
            return pc is null ? Results.NotFound() : Results.Ok(pc);
        }).RequireAuthorization();

        companyInfo.MapPost("/cases", async (CreateProjectCaseRequest req, CreateProjectCaseCommandHandler handler, ClaimsPrincipal user, CancellationToken ct) =>
        {
            if (!user.TryGetUserId(out var userId)) return Results.Unauthorized();

            try
            {
                var pc = await handler.Handle(new CreateProjectCaseCommand(req.ClientName, req.Text, req.IsActive), userId, ct);
                return Results.Created($"/settings/company-info/cases/{pc.Id}", pc);
            }
            catch (UnauthorizedAccessException) { return Results.StatusCode(403); }
        }).RequireAuthorization();

        companyInfo.MapPut("/cases/{id:guid}", async (Guid id, UpdateProjectCaseRequest req, UpdateProjectCaseCommandHandler handler, ClaimsPrincipal user, CancellationToken ct) =>
        {
            if (!user.TryGetUserId(out var userId)) return Results.Unauthorized();

            try
            {
                var pc = await handler.Handle(new UpdateProjectCaseCommand(id, req.ClientName, req.Text, req.IsActive), userId, ct);
                return pc is null ? Results.NotFound() : Results.Ok(pc);
            }
            catch (UnauthorizedAccessException) { return Results.StatusCode(403); }
        }).RequireAuthorization();

        companyInfo.MapDelete("/cases/{id:guid}", async (Guid id, DeleteProjectCaseCommandHandler handler, ClaimsPrincipal user, CancellationToken ct) =>
        {
            if (!user.TryGetUserId(out var userId)) return Results.Unauthorized();

            try
            {
                var deleted = await handler.Handle(new DeleteProjectCaseCommand(id), userId, ct);
                return deleted ? Results.NoContent() : Results.NotFound();
            }
            catch (UnauthorizedAccessException) { return Results.StatusCode(403); }
        }).RequireAuthorization();
    }
}
