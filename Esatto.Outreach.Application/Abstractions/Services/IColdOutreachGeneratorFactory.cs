using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Application.Abstractions.Services;

public interface IColdOutreachGeneratorFactory
{
    IColdOutreachGenerator GetGenerator(OutreachGenerationType? type = null);
}
