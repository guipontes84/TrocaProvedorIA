using Microsoft.Extensions.AI;
using OpenAI;

// Exemplo: o serviço depende só de IChatClient. O provedor é escolhido aqui, no registro.
// Escolha com a variável PROVEDOR: "anthropic", "openai" ou "teste" (padrão, sem rede).

var provedor = Environment.GetEnvironmentVariable("PROVEDOR") ?? "teste";
IChatClient cliente = CriarCliente(provedor);

var servico = new ResumidorService(cliente);
var resumo = await servico.ResumirAsync("IChatClient é a abstração do Microsoft.Extensions.AI para conversar com modelos de IA.");
Console.WriteLine($"[{provedor}] {resumo}");

// Único lugar que conhece os SDKs dos provedores.
static IChatClient CriarCliente(string provedor) => provedor switch
{
    "anthropic" => new Anthropic.AnthropicClient().AsIChatClient("claude-sonnet-5-5"),
    "openai" => new OpenAIClient(Environment.GetEnvironmentVariable("OPENAI_API_KEY"))
        .GetChatClient(Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? "gpt-4o-mini")
        .AsIChatClient(),
    _ => new ClienteFalso(),
};

public class ResumidorService(IChatClient chat)
{
    public async Task<string> ResumirAsync(string texto)
    {
        var resposta = await chat.GetResponseAsync(
            [new ChatMessage(ChatRole.User, $"Resuma em 3 frases:\n{texto}")]);
        return resposta.Text;
    }
}

// Cliente de teste: devolve uma resposta fixa, sem chamar API nenhuma.
public class ClienteFalso : IChatClient
{
    public ChatClientMetadata Metadata { get; } = new("ClienteFalso");

    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
        Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "Resposta de teste, sem chamada à API.")));

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield return new ChatResponseUpdate(ChatRole.Assistant, "Resposta de teste.");
        await Task.CompletedTask;
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose() { }
}
