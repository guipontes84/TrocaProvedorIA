# LinkedinDicas

Gera rascunhos de posts para o LinkedIn sobre **IA x C#** (Anthropic, OpenAI, LLMs, RAG, Skills, .NET/C#, Microsoft Agent Framework), três vezes por dia.

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
