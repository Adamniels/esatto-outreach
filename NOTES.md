Jag funderar på om man i framtiden ska ha att man samlar och sparar information om ett företag i en databas som man sen kan använda för att skriva ett mejl, 
eller om det helt enkelt bara är bättre att låta AI göra research och skriva mejlet i samma steg, den kankse får bättre context då.

## Features som jag vill ha, som inte är mvp
- [ ] kunna chatta med en ai som ändrar mejlet live

# Det verkar behöva vara i development environment för att user-secrets ska funka

```python
# Ett fel är att man ska använda prev id inte bara det första varje gång
from openai import OpenAI

client = OpenAI(api_key="OPENAIKEY")

# 1) Tom konversation
conversation = client.conversations.create()
print("Conversation ID:", conversation.id)

# 2) Första turen – MÅSTE skicka input i första responses.create
r1 = client.responses.create(
    model="gpt-4.1",
    input=[{"role": "user", "content": "What are the 5 Ds of dodgeball?"}],
    conversation=conversation.id,
)
print("\\nResponse 1:\\n", r1.output_text)

# 3) Fortsätt i samma konversation
r2 = client.responses.create(
    model="gpt-4.1",
    input=[{"role": "user", "content": "And who said that quote?"}],
    conversation=conversation.id,
)
print("\\nResponse 2:\\n", r2.output_text)

```
