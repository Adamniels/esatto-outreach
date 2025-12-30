Plan: Add OpenAI Web Search Endpoint for Soft Company Data
Add a new endpoint that generates soft company data via OpenAI with web search 
capability, and integrate this data into the existing prospect retrieval endpoint.

Steps
Create SoftCompanyDataDto in Esatto.Outreach.Application/DTOs/ProspectDtos.cs 
with properties matching SoftCompanyData entity (PersonalizationHooks, Events, 
News, SocialMedia, Sources, ResearchDate) plus Id and timestamps

Create IOpenAIWebSearchClient interface in 
Esatto.Outreach.Application/Abstractions/IOpenAIWebSearchClient.cs 
with method GenerateSoftCompanyDataAsync(ProspectInfoDto) 
returning structured soft data matching SoftCompanyData entity properties

Implement GenerateSoftCompanyData use case in 
Esatto.Outreach.Application/UseCases/GenerateSoftCompanyData.cs 
that orchestrates: fetch prospect via IProspectsRepository, 
call OpenAI service, create/update SoftCompanyData entity, 
link to prospect, save via ISoftCompanyDataRepository, 
return SoftCompanyDataDto

Implement OpenAIWebSearchClient in 
Esatto.Outreach.Infrastructure/OpenAI/OpenAIWebSearchClient.cs 
following OpenAIChatService pattern with web search tool enabled, 
prompt engineering for research, and JSON parsing for soft data fields

Add POST /prospects/{id:guid}/soft-data/generate endpoint in 
Esatto.Outreach.Api/Program.cs that invokes GenerateSoftCompanyData 
use case and returns SoftCompanyDataDto with appropriate error handling

Modify ProspectViewDto.FromEntity in 
Esatto.Outreach.Application/DTOs/ProspectDtos.cs to include optional 
SoftCompanyDataDto? property and map from Prospect.SoftCompanyData 
navigation property if present

Update GetProspectById use case in 
Esatto.Outreach.Application/UseCases/GetProspectById.cs to include 
soft data navigation when fetching prospect entity so DTO mapping 
includes it

Register new services in
Esatto.Outreach.Infrastructure/DependencyInjection.cs adding 
IOpenAIWebSearchClient → OpenAIWebSearchClient and GenerateSoftCompanyData 
use case as scoped services

Further Considerations
Caching strategy: Should the endpoint regenerate if soft data already 
exists, or check IsDataStale() first? Add a force-refresh query parameter?

OpenAI configuration: Reuse existing OpenAISettings or add separate 
configuration for web search model/parameters (temperature, max tokens)?

Repository eager loading: Should IProspectsRepository.GetByIdAsync 
add .Include(p => p.SoftCompanyData) by default, or create separate method 
like GetByIdWithSoftDataAsync?
