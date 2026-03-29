using Esatto.Outreach.Domain.Entities;
using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Application.DTOs.Prospects;

public record PendingProspectDto(
    Guid Id,
    string Name,
    CrmProvider CrmSource,
    string? ExternalCrmId,
    string? About,
    string? PictureURL,
    List<WebsiteDto> Websites,
    DateTime CreatedUtc)
{
    public static PendingProspectDto FromEntity(Prospect p)
    {
        if (!p.IsPending)
            throw new InvalidOperationException("Cannot create PendingProspectDto from non-pending prospect");

        return new(
            p.Id,
            p.Name,
            p.CrmSource,
            p.ExternalCrmId,
            p.About,
            p.PictureURL,
            p.Websites?.Select(w => new WebsiteDto(w.Url, w.Service, w.Type)).ToList() ?? new(),
            p.CreatedUtc);
    }
}
