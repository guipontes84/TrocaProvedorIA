# CLAUDE.md

Instruções para o Claude ao trabalhar neste repositório. Leia também [docs/SDD.md](docs/SDD.md) para o desenho do sistema e [docs/VISAO.md](docs/VISAO.md) para o objetivo em linguagem simples.

## Projeto

Gerador de rascunhos de posts do LinkedIn sobre IA x C#/.NET. Console app em .NET 10 (`Program.cs`), com exemplo em `examples/ResumidorExemplo/`.

## Comandos

```powershell
dotnet build
dotnet run --project .\LinkedinDicas.csproj
dotnet run --project .\examples\ResumidorExemplo\ResumidorExemplo.csproj
```

Chave de API: variável de ambiente `ANTHROPIC_API_KEY`. Nunca escreva a chave no código ou em arquivos versionados.

## Regras

- **Não publique nada externamente.** Posts, mensagens e agendamentos em redes sociais só com confirmação explícita.
- **Não invente fatos.** Números, datas, nomes de recursos e citações precisam vir da fonte. Se faltar informação, diga o que é conhecido.
- **Textos gerados em português do Brasil**, no tom descrito em `Program.cs` (gancho, listas com 🔹/✅, dica prática, convite final).
- **Assinatura e dados pessoais** ficam em `config/`, nunca fixos no código.
- **Rascunhos em `drafts/`** são artefatos de saída. Não edite à mão os arquivos gerados automaticamente sem motivo.
- **Não crie `Program.cs.bak` nem arquivos temporários** dentro do projeto; use a pasta de scratchpad.

## Convenções

- Código em C#, com nomes de classes e métodos em inglês quando técnicos e em português quando forem conceitos do domínio do post (`ResumirAsync`, `GerarPostAsync`).
- Mensagens ao usuário em português.
- Antes de dizer que algo funciona, rode o build e, quando possível, a execução.

## Documentação padrão

Todo projeto novo deste repositório tem estes documentos:

| Arquivo | Público | Conteúdo |
|---|---|---|
| `README.md` | Quem vai rodar | Como instalar, configurar e executar. |
| `docs/SDD.md` | Quem vai desenvolver | Escopo, arquitetura, decisões, riscos e questões em aberto. |
| `docs/VISAO.md` | Qualquer pessoa | Objetivo, valor e resultados esperados, sem termos técnicos. |
| `CLAUDE.md` | Claude | Comandos, regras e convenções. |

Ao mudar o comportamento do projeto, atualize o SDD e o README na mesma tarefa.
