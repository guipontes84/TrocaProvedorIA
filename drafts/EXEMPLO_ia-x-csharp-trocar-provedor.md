<!-- EXEMPLO de rascunho, criado manualmente para revisão. Não é notícia real. -->

Trocar de provedor de IA não deveria obrigar você a reescrever a aplicação.

No .NET, a biblioteca Microsoft.Extensions.AI define a interface IChatClient. Seu código conversa com essa interface, e o provedor fica por trás dela.

Por que isso importa:

🔹 Seu código de negócio não depende de um SDK específico
🔹 Você testa com um cliente falso, sem chamar a API de verdade
🔹 Trocar de Anthropic para OpenAI fica concentrado em um lugar só: o registro do cliente
✅ Menos acoplamento, mais liberdade para comparar modelos

Exemplo em C#:

```csharp
public class ResumidorService(IChatClient chat)
{
    public async Task<string> ResumirAsync(string texto)
    {
        var resposta = await chat.GetResponseAsync(
            [new ChatMessage(ChatRole.User, $"Resuma em 3 frases:\n{texto}")]);
        return resposta.Text;
    }
}
```

Quem registra o provedor concreto é o Program.cs. A classe ResumidorService não precisa saber qual é.

Na hora de testar, basta passar um IChatClient falso para o serviço. Sem rede, sem chave de API, sem custo.

E você, já usa alguma abstração para não ficar preso a um provedor de IA? Conta nos comentários.

#dotnet #csharp #ia #microsoftextensionsai #arquitetura #llm
