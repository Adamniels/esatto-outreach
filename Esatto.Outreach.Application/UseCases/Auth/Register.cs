using Esatto.Outreach.Application.Abstractions;
using Esatto.Outreach.Application.DTOs.Auth;
using Esatto.Outreach.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Esatto.Outreach.Application.UseCases.Auth;

/// <summary>
/// Register a new user and return JWT tokens.
/// </summary>
public sealed class Register
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenService _jwtService;
    private readonly IRefreshTokenRepository _refreshTokenRepo;
    private readonly IGenerateEmailPromptRepository _promptRepo;
    private readonly ICompanyRepository _companyRepo;
    private readonly ICompanyInfoRepository _companyInfoRepo;
    private readonly IUnitOfWork _unitOfWork;

    // TODO: Esatto hardcoded prompt, should be removed/changed
    private const string DEFAULT_PROMPT = @"Fokusera på hur vi (Esatto AB) kan hjälpa företaget. 
Använd informationen ovan om Esatto för att:
- Hitta relevanta cases som liknar kundens bransch eller utmaningar
- Visa konkret förståelse för kundens behov genom att referera till liknande projekt
- Matcha rätt tjänster och metoder till kundens situation
- Skriv i Esattos ton och värderingar (ärlighet, engagemang, omtanke, samarbete)
- Använd inga '-', inga bindestreck

Krav:
- Hook i första meningen.
- 1–2 konkreta värdeförslag anpassade till företaget.
- Referera gärna till ett eller två relevant Esatto-case som exempel
- Avsluta med en enkel call-to-action (t.ex. 'Vill du att jag skickar ett konkret förslag?').";

    public Register(
        UserManager<ApplicationUser> userManager,
        IJwtTokenService jwtService,
        IRefreshTokenRepository refreshTokenRepo,
        IGenerateEmailPromptRepository promptRepo,
        ICompanyRepository companyRepo,
        ICompanyInfoRepository companyInfoRepo,
        IUnitOfWork unitOfWork)
    {
        _userManager = userManager;
        _jwtService = jwtService;
        _refreshTokenRepo = refreshTokenRepo;
        _promptRepo = promptRepo;
        _companyRepo = companyRepo;
        _companyInfoRepo = companyInfoRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<(bool Success, AuthResponseDto? Data, string? Error)> Handle(
        RegisterRequestDto request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.CompanyName))
            return (false, null, "Company name is required");

        var existingCompany = await _companyRepo.GetByNameAsync(request.CompanyName.Trim());
        if (existingCompany != null)
            return (false, null, "Company name already exists");

        await using var transaction = await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            var company = new Company { Name = request.CompanyName.Trim() };
            await _companyRepo.AddAsync(company, ct);

            // Automatically create empty CompanyInformation for the new company
            var companyInfo = new CompanyInformation
            {
                CompanyId = company.Id,
                Overview = "Din företagspresentation här...",
                ValueProposition = "Ditt värdeerbjudande här..."
            };
            await _companyInfoRepo.AddAsync(companyInfo, ct);

            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
                return (false, null, "Email already registered");

            // Create user
            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                FullName = request.FullName,
                CreatedUtc = DateTime.UtcNow,
                CompanyId = company.Id,
            };


            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return (false, null, $"Registration failed: {errors}");
            }
            
            await _unitOfWork.CommitTransactionAsync(ct);

            // Create default email prompt for the new user
            var defaultPrompt = GenerateEmailPrompt.Create(user.Id, DEFAULT_PROMPT, isActive: true);
            await _promptRepo.AddAsync(defaultPrompt, ct);
            
            // Generate tokens
            var (accessToken, expiresAt) = _jwtService.GenerateAccessToken(user);
            var refreshToken = _jwtService.GenerateRefreshToken();

            // Store refresh token
            await _refreshTokenRepo.AddAsync(new RefreshToken
            {
                Token = refreshToken,
                UserId = user.Id,
                ExpiresAt = _jwtService.GetRefreshTokenExpiryDate()
            }, ct);

            var response = new AuthResponseDto(
                accessToken,
                refreshToken,
                expiresAt,
                new UserDto(user.Id, user.Email!, user.FullName)
            );

            return (true, response, null);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(ct);
            throw;
        }

    }
}
