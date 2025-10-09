using Esatto.Outreach.Domain.Entities;
using Esatto.Outreach.Application.DTOs;
using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Application.Abstractions;

public interface ICustomEmailGenerator
{
    Task<CustomEmailDraftDto> GenerateAsync(
              CustomEmailRequestDto request,
              CancellationToken cancellationToken = default);
}
