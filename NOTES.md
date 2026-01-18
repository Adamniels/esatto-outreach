# Development Notes

## Future Considerations
I'm considering whether in the future we should collect and save information about a company in a database that can then be used to write an email, or if it's simply better to let AI do research and write the email in the same step - it might get better context that way.

## Features I want (not MVP)
- [ ] Ability to chat with an AI that modifies the email live

# User Secrets Configuration
It appears you need to be in the development environment for user-secrets to work.
Run:
```bash
DOTNET_ENVIRONMENT=Development dotnet run --project Esatto.Outreach.Api
```

## OpenAI Conversation API Example
```python
# Note: You need to use the previous ID, not just the first one every time
from openai import OpenAI

client = OpenAI(api_key="OPENAIKEY")

# 1) Empty conversation
conversation = client.conversations.create()
print("Conversation ID:", conversation.id)

# 2) First turn – MUST send input in the first responses.create
r1 = client.responses.create(
    model="gpt-4.1",
    input=[{"role": "user", "content": "What are the 5 Ds of dodgeball?"}],
    conversation=conversation.id,
)
print("\\nResponse 1:\\n", r1.output_text)

# 3) Continue in the same conversation
r2 = client.responses.create(
    model="gpt-4.1",
    input=[{"role": "user", "content": "And who said that quote?"}],
    conversation=conversation.id,
)
print("\\nResponse 2:\\n", r2.output_text)
```

# Capsule CRM Webhook Setup (Development)

## ngrok Issue
⚠️ **Every time you start ngrok you get a NEW random URL** (e.g., `https://abc123.ngrok.io`)

This means you must:
1. Update the webhook URL in Capsule CRM on every restart
2. Or pay for ngrok Pro (~$8/month) for a fixed subdomain

## Step-by-step Guide for Development

### 1. Start Backend API
```bash
cd /Users/adamnielsen/Projects/Esatto-project-outreach/esatto-outreach/Esatto.Outreach.Api
dotnet run
```