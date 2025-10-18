using Esatto.Outreach.Domain.Entities;
using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Application.DTOs;

public record CustomEmailRequestDto(
    string CompanyName,
    string? Domain,
    string? ContactName,
    string? ContactEmail,
    string? LinkedinUrl,
    string? Notes
);


public record CustomEmailDraftDto(
   string Title,
   string BodyPlain,
   string BodyHTML
);


public record SendOutreachToN8nDTO(
   string To,
   string Subject,
   string Body
);

public record ResponseSendOutreachToN8nDTO(
        bool Success,
        string? Message
);
