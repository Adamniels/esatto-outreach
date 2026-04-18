using Esatto.Outreach.Application.Abstractions.Repositories;

namespace Esatto.Outreach.Application.Features.Auth.ValidateInvitation;

public sealed class ValidateInvitationCommandHandler
{
    private readonly IInvitationRepository _invitationRepo;

    public ValidateInvitationCommandHandler(IInvitationRepository invitationRepo)
    {
        _invitationRepo = invitationRepo;
    }

    public async Task<ValidateInvitationResponse?> Handle(ValidateInvitationCommand command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.Token))
            return null;

        var hashedToken = ComputeSha256(command.Token.Trim());

        var invitation = await _invitationRepo.GetByTokenAsync(hashedToken, ct);
        if (invitation == null)
            return null;
        if (invitation.UsedAt != null)
            return null;
        if (invitation.ExpiresAt < DateTime.UtcNow)
            return null;

        return new ValidateInvitationResponse(invitation.Company.Name, invitation.Email);
    }

    private static string ComputeSha256(string input)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        var hash = System.Security.Cryptography.SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
}
