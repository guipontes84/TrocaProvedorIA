# LinkedinDicas

Gera rascunhos de posts para o LinkedIn sobre **IA x C#** (Anthropic, OpenAI, LLMs, RAG, Skills, .NET/C#, Microsoft Agent Framework), três vezes por dia.

![Trocar de provedor de IA sem reescrever o código](drafts/EXEMPLO_ia-x-csharp-trocar-provedor.svg?v=2)

Exemplo de ilustração gerada: a aplicação depende de `IChatClient`, e o provedor (Anthropic, OpenAI ou cliente de teste) fica atrás dessa interface.

## Conceitos utilizados

**Princípios SOLID** (detalhados em [docs/SDD.md](docs/SDD.md)):
- **S**, responsabilidade única: `ResumidorService` só resume; `CriarCliente` só monta o cliente.
- **O**, aberto/fechado: parcial. Adicionar provedor exige editar o `switch` em `CriarCliente`.
- **L**, substituição de Liskov: `ClienteFalso` substitui `IChatClient` sem o serviço notar.
- **I**, segregação de interfaces: o serviço usa só `GetResponseAsync`.
- **D**, inversão de dependência: o serviço depende da abstração `IChatClient`.

**Padrões de projeto:**
- **Adapter:** `AsIChatClient(...)` adapta os SDKs da Anthropic e da OpenAI para `IChatClient`.
- **Strategy:** trocar o `IChatClient` troca o comportamento do serviço, sem mudar o serviço.
- **Factory (simples):** `CriarCliente` escolhe o cliente a partir de `PROVEDOR`.
- **Injeção de dependência:** o construtor recebe o cliente; `Program.cs` é o *composition root*.
- **Fake:** `ClienteFalso` substitui o provedor real nos testes.

**Ideia de DDD:** camada anticorrupção. `IChatClient` é a porta que o domínio conhece, e os SDKs dos provedores ficam atrás dela como adaptadores.

Cada rascunho sai com:
- `drafts/<data>_<titulo>.md`: texto do post em português, com gancho, lista, comparação ou exemplo em C#, e dica de uma das áreas (arquitetura, APIs/microsserviços, mensageria, cloud/DevOps, bancos, front-end).
- `drafts/<data>_<titulo>.svg`: ilustração do ponto principal. Exporte para PNG antes de publicar no LinkedIn.

## Documentação

- [docs/VISAO.md](docs/VISAO.md): o que é o projeto e por quê, sem termos técnicos.
- [docs/SDD.md](docs/SDD.md): arquitetura, decisões, riscos e questões em aberto.
- [CLAUDE.md](CLAUDE.md): comandos e regras para o Claude trabalhar no repositório.
- [examples/ResumidorExemplo](examples/ResumidorExemplo): exemplo básico de `IChatClient`.

O projeto **não publica nada sozinho**. Você revisa os rascunhos e posta manualmente.

## Configuração

1. Tenha o .NET 10 SDK instalado.
2. Defina sua chave da API Anthropic (uma vez, no PowerShell):

```powershell
[Environment]::SetEnvironmentVariable("ANTHROPIC_API_KEY", "SUA_CHAVE", "User")
```

3. Sua assinatura: edite `config/signature.txt` e cole o texto. Ela é adicionada ao final de cada post. Deixe vazio se não quiser.
4. Fontes: `config/feeds.json`. Confirme que as URLs de RSS estão ativas antes da primeira execução. Feeds que falharem aparecem como `[aviso]` no log e não interrompem a execução.
5. Agendamento (3x ao dia, 08:00, 13:00 e 19:00):

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\agendar-tarefas.ps1
```

## Execução manual

```powershell
dotnet run --project .\LinkedinDicas.csproj
```

Variáveis opcionais: `ANTHROPIC_MODEL` (padrão `claude-sonnet-5-5`) e `MAX_ITEMS_POR_EXECUCAO` (padrão `3`).

## Como funciona

- Lê os feeds, mantém só itens das últimas 72h.
- Ignora links já usados (`state/vistos.json`), então não repete novidades.
- Para cada item, pede ao modelo o texto e o SVG num formato fixo (`===POST===` / `===SVG===`).
- Salva os arquivos em `drafts/`.

## Próximos passos possíveis

- Exportar SVG para PNG automaticamente.
- Publicar via API do LinkedIn (exige aprovação de app na plataforma LinkedIn).
