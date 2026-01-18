# AI-Driven B2B Enrichment Strategy

## Overview
A self-built, AI-first enrichment pipeline designed to generate high-quality, human-like outreach personalization without relying on expensive third-party data aggregators (like Apollo/Clearbit).

**Core Philosophy**: 
- **Quality over Quantity**: We simulate how a human researcher would manually investigate a prospect.
- **AI as the Researcher**: We use LLMs with Web Browsing capabilities to "read" websites and LinkedIn profiles.
- **Merge & Conquer**: Combine company research and contact finding into a single intelligent step.

---

## The 2-Phase Workflow

### Phase 1: Company Intelligence & Contact Scouting (Layers 1 & 2)
**Goal**: Understand the business, find a "Company News Hook", and identify the Buying Committee.

**Inputs**:  
`Domain` (e.g., `esatto.se`) OR `Company Name`

**AI Agent Workflow**:
1.  **Website Deep Dive**:
    *   Agent visits company homepage + "About Us" + "Services".
    *   *Extracts*: Value proposition, Industry, Tech Stack hints.
2.  **News & Signal Search**:
    *   Agent searches: `"[Company Name] recent news"`, `"[Company Name] press release"`, `"[Company Name] LinkedIn recent posts"`.
    *   *Extracts*: "I saw you recently [launched X / raised Y / hired Z]..." (The Company Hook).
3.  **Contact Discovery (via Search)**:
    *   Agent searches: `"site:linkedin.com/in/ [Company Name] [Job Title Keywords]"`
    *   *Keywords*: "CTO", "Marketing Manager", "CEO", "Head of Digital".
    *   *Extracts*: Name, Job Title, LinkedIn Profile URL.

**Output Data**:
*   `CompanySummary`: "A digital agency in Sweden focusing on..."
*   `CompanyHooks`: ["Launched a new AI service in Q3", "Opened a new office in Gothenburg"]
*   `ProspectCandidates`: List of { Name, Role, ProfileUrl, ConfidenceScore }

---

### Phase 2: Personal Deep Dive (Layer 3)
**Goal**: Find a hyper-personalized "Human Hook" for the specific identified prospect.

**Inputs**:  
`LinkedIn Profile URL` (from Phase 1) + `Name`

**AI Agent Workflow**:
1.  **Profile Synthesis**:
    *   Agent searches public profile data / cached versions.
    *   *Extracts*: Past roles, Education, About section summary.
2.  **Activity & Voice Scan**:
    *   Agent searches: `"[Name] [Company] linkedin post"`, `"[Name] podcast interview"`, `"[Name] article"`.
    *   *Extracts*: Recent thoughts, commenting style, specific topics they care about.
3.  **Hook Generation**:
    *   Synthesize a "Icebreaker" that connects the *Company Hook* (Phase 1) with the *Personal Interest* (Phase 2).
    *   *Example*: "I listened to your deeper dive into [Topic] on the [Podcast Name]..."

**Output Data**:
*   `PersonalHooks`: ["Posted about AI ethics last week", "Used to work at [Competitor]", "Fan of [Specific Tech]"]
*   `SuggestedOpeningLine`: "Hi Adam, saw your post about AI agents..."

---

## Technical Implementation Plan

### 1. The `GlobalResearchAgent` (Phase 1)
Instead of simple API calls, we treat this as a multi-step agent task.
*   **Tool**: `OpenAIWebSearchClient` (Custom).
*   **Prompting**: Needs to be robust. "You are a biz dev researcher. Find the most relevant news. Do not hallucinate."
*   **Storage**: Save results to `Prospect` entities, possibly creating new `SoftCompanyData` records.

### 2. The `PersonalReconAgent` (Phase 2)
*   **Context**: Needs access to the Phase 1 output to avoid repeating generic company info.
*   **Privacy Guardrails**: Explicit instructions to ignore personal private info (family, kids) and focus on *professional* digital footprint.

### 3. Data Verification
*   Since we are "guessing" emails or finding them via search, we need a verification step (e.g., standard email format `firstname.lastname@company.com` + Verification Tool).

## Open Questions & Risks
1.  **LinkedIn Rate Limiting**: Searching `site:linkedin.com` is safer than scraping, but heavy volume might still trigger CAPTCHAs on the search engine side (Google/Bing).
2.  **Accuracy**: LLMs might hallucinate a "CEO" specific to a subsidiary rather than the group. How do we manually verify before outreach?
3.  **Cost**: Multiple GPT-4o steps with browsing are expensive per lead. Is this acceptable for high-ticket sales?
