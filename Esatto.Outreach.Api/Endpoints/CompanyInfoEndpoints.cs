using Esatto.Outreach.Application.UseCases.CompanyInfo;

namespace Esatto.Outreach.Api.Endpoints;

public static class CompanyInfoEndpoints
{
    public static void MapCompanyInfoEndpoints(this WebApplication app)
    {
        var companyInfo = app.MapGroup("/settings/company-info").WithTags("Company Info");

        // Get company info (read-only)
        companyInfo.MapGet("/", async (GetCompanyInfo useCase, CancellationToken ct) =>
        {
            try
            {
                var info = await useCase.Handle(ct);
                return Results.Ok(info);
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Failed to load company info"
                );
            }
        }).RequireAuthorization();
    }
}
