using System.Text.Json.Serialization;

namespace Esatto.Outreach.Infrastructure.EmailGeneration;

/// <summary>
/// Request model for Esatto RAG email generation API
/// </summary>
public sealed record EsattoRagRequest
{
    [JsonPropertyName("company_name")]
    public required string CompanyName { get; init; }

    [JsonPropertyName("documents")]
    public required List<RagDocument> Documents { get; init; }

    [JsonPropertyName("preferences")]
    public required RagPreferences Preferences { get; init; }

    [JsonPropertyName("contact_person")]
    public required string ContactPerson { get; init; }
}

/// <summary>
/// Document to be used in RAG context
/// </summary>
public sealed record RagDocument
{
    [JsonPropertyName("doc_type")]
    public required string DocType { get; init; }

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("text")]
    public required string Text { get; init; }

    [JsonPropertyName("source_url")]
    public required string SourceUrl { get; init; }

    [JsonPropertyName("priority")]
    public required double Priority { get; init; }
}

/// <summary>
/// Preferences for email generation
/// </summary>
public sealed record RagPreferences
{
    [JsonPropertyName("tone")]
    public required string Tone { get; init; }

    [JsonPropertyName("language")]
    public required string Language { get; init; }

    [JsonPropertyName("max_length")]
    public required int MaxLength { get; init; }

    [JsonPropertyName("focus_areas")]
    public List<string> FocusAreas { get; init; } = new();
}

/// <summary>
/// Response from Esatto RAG email generation API
/// </summary>
public sealed record EsattoRagResponse
{
    [JsonPropertyName("subject")]
    public required string Subject { get; init; }

    [JsonPropertyName("body")]
    public required string Body { get; init; }

    [JsonPropertyName("metadata")]
    public RagMetadata? Metadata { get; init; }
}

/// <summary>
/// Metadata about the email generation process
/// </summary>
public sealed record RagMetadata
{
    [JsonPropertyName("retrieved_esatto_cases")]
    public List<string> RetrievedEsattoCases { get; init; } = new();

    [JsonPropertyName("company_chunks_used")]
    public int CompanyChunksUsed { get; init; }

    [JsonPropertyName("generation_time_ms")]
    public int GenerationTimeMs { get; init; }

    [JsonPropertyName("embedding_model")]
    public string EmbeddingModel { get; init; } = string.Empty;

    [JsonPropertyName("llm_model")]
    public string LlmModel { get; init; } = string.Empty;
}
