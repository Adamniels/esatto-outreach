using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace Esatto.Outreach.Domain.ValueObjects;

// Root Result
public class CompanyEnrichmentResult
{
    public CompanySnapshot Snapshot { get; set; } = new();
    public List<EvidenceSource> EvidenceLog { get; set; } = new();
    public BusinessChallenges Challenges { get; set; } = new();
    public SolutionRelevantProfile Profile { get; set; } = new();
    public List<CompanyOutreachHook> OutreachHooks { get; set; } = new();
    public List<string> MethodologyUsed { get; set; } = new();
    public List<string> OpenQuestions { get; set; } = new();
}

// A) Company Snapshot
public class CompanySnapshot
{
    public string WhatTheyDo { get; set; } = "";
    public string HowTheyOperate { get; set; } = "";
    public string TargetCustomer { get; set; } = "";
    public string PrimaryValueProposition { get; set; } = "";
}

// B) Evidence Log
public class EvidenceSource
{
    public string Title { get; set; } = "";
    public string? Publisher { get; set; }
    public string? Date { get; set; } 
    public string Url { get; set; } = "";
    public string KeyFactExtracted { get; set; } = "";
}

// C) Business Challenges
public class BusinessChallenges
{
    public List<ConfirmedChallenge> Confirmed { get; set; } = new();
    public List<InferredChallenge> Inferred { get; set; } = new();
}

public class ConfirmedChallenge
{
    public string ChallengeDescription { get; set; } = "";
    public string EvidenceSnippet { get; set; } = "";
    public string SourceUrl { get; set; } = "";
}

public class InferredChallenge
{
    public string ChallengeDescription { get; set; } = "";
    public string Reasoning { get; set; } = "";
    public string ConfidenceLevel { get; set; } = "";
}

// D) Solution-Relevant Company Profile
public class SolutionRelevantProfile
{
    public string BusinessModel { get; set; } = "";
    public string RevenueMotion { get; set; } = "";
    public string CustomerType { get; set; } = "";
    public string OperationalComplexity { get; set; } = "";
    public string OperationalComplexityReasoning { get; set; } = "";
    public string DataIntegrationNeeds { get; set; } = "";
    public string ScalingStage { get; set; } = "";
    public string ComplianceContext { get; set; } = "";
    public string TechnologyPosture { get; set; } = ""; 
    public List<string> CurrentTechStack { get; set; } = new();
    public List<string> HiringTrends { get; set; } = new();
    public List<string> StrategicPriorities { get; set; } = new();
    public string ProcessMaturity { get; set; } = "";
    public string? NotableConstraints { get; set; }
    public Dictionary<string, string> FieldConfidence { get; set; } = new();
}

// E) Personalization Hooks
public class CompanyOutreachHook
{
    public string HookDescription { get; set; } = "";
    public string? Date { get; set; }
    public string Source { get; set; } = "";
    public string WhyItMatters { get; set; } = "";
    public string ConfidenceLevel { get; set; } = "";
}
