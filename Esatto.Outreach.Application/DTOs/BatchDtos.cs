namespace Esatto.Outreach.Application.DTOs;

/// <summary>
/// Result of a batch operation containing both successes and failures
/// </summary>
public record BatchOperationResultDto<TData>
{
    public List<SuccessResult<TData>> Successes { get; init; } = new();
    public List<FailureResult> Failures { get; init; } = new();
    
    public int TotalCount => Successes.Count + Failures.Count;
    public int SuccessCount => Successes.Count;
    public int FailureCount => Failures.Count;
}

/// <summary>
/// Successful batch operation result for a single prospect
/// </summary>
public record SuccessResult<TData>(Guid ProspectId, TData Data);

/// <summary>
/// Failed batch operation result for a single prospect
/// </summary>
public record FailureResult(Guid ProspectId, string ErrorMessage);

/// <summary>
/// Request to generate soft data for multiple prospects
/// </summary>
public record BatchSoftDataRequest(
    List<Guid> ProspectIds,
    string? Provider = null // "OpenAI", "Claude", or "Hybrid"
);

/// <summary>
/// Request to generate emails for multiple prospects
/// </summary>
public record BatchEmailRequest(
    List<Guid> ProspectIds,
    string? Type = null, // "WebSearch" or "UseCollectedData"
    bool AutoGenerateSoftData = true, // If true and Type=UseCollectedData, auto-generate missing soft data
    string? SoftDataProvider = "Claude" // Provider to use for auto-generation (default: Claude)
);
