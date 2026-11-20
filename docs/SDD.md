# SDD: LinkedinDicas

Documento de design do projeto. Descreve o que o sistema faz, como ele está organizado e quais decisões foram tomadas.

**Status:** rascunho v1
**Última revisão:** 2026-11-19

---

## 1. Objetivo

Gerar, três vezes por dia, rascunhos de posts em português para o LinkedIn sobre IA aplicada ao ecossistema .NET/C#. Cada rascunho traz texto e uma ilustração SVG do mesmo tema.

## 2. Escopo

**Dentro do escopo**
- Coletar novidades de feeds RSS/Atom configurados.
- Filtrar itens recentes (últimas 72h) e ainda não usados.
- Gerar texto e SVG com um modelo de linguagem.
- Salvar os resultados em `drafts/`.
- Agendar a execução em três horários no Agendador do Windows.

**Fora do escopo (por enquanto)**
- Publicar automaticamente no LinkedIn. A publicação é manual ou, no futuro, via plugin de agendamento, sempre com confirmação.
- Converter SVG para PNG automaticamente.
- Interface gráfica.

## 3. Arquitetura

```
config/feeds.json ──► Coleta RSS/Atom ──► Filtro (72h, não visto)
                                              │
config/signature.txt ─────────────┐           ▼
                                  └──► Modelo de linguagem (texto + SVG)
                                              │
                                              ▼
                              drafts/*.md  +  drafts/*.svg
                                              │
                              state/vistos.json (evita repetição)
```

**Componentes**

| Caminho | Função |
|---|---|
| `Program.cs` | Fluxo completo: coleta, filtro, geração, gravação. |
| `config/feeds.json` | Lista de fontes (nome, URL, temas). |
| `config/signature.txt` | Assinatura anexada ao fim de cada post. Pode ficar vazia. |
| `drafts/` | Saída: um `.md` e um `.svg` por novidade. |
| `state/vistos.json` | Links já processados. |
| `scripts/agendar-tarefas.ps1` | Cria as três tarefas agendadas. |
| `examples/ResumidorExemplo/` | Exemplo básico de `IChatClient` para os posts. |

### 3.1 Princípios SOLID aplicados

Aplicados no exemplo `examples/TrocaDeProvedorIA/`. O gerador (`Program.cs`) segue o mesmo desenho, mas com menos separação.

| Princípio | Onde aparece | Limite atual |
|---|---|---|
| **S** (responsabilidade única) | `ResumidorService` só resume texto; `CriarCliente` só monta o cliente. | Nenhum. |
| **O** (aberto/fechado) | Parcial: adicionar um provedor exige editar o `switch` de `CriarCliente`. | Não é totalmente aberto. Um registro por dicionário, sem alterar o código existente, resolveria. |
| **L** (substituição de Liskov) | `ClienteFalso` substitui `IChatClient` sem o serviço notar a diferença. | Nenhum. |
| **I** (segregação de interfaces) | O serviço usa só `GetResponseAsync` de `IChatClient`. | Nenhum. |
| **D** (inversão de dependência) | `ResumidorService` depende da abstração `IChatClient`. Só `CriarCliente` conhece os SDKs. | Nenhum. |

### 3.2 Padrões de projeto aplicados

Aplicados no exemplo `examples/TrocaDeProvedorIA/`.

| Padrão | Onde aparece | Observação |
|---|---|---|
| **Adapter** | `AsIChatClient(...)` adapta os SDKs da Anthropic e da OpenAI para `IChatClient`. | Uso mais direto do padrão no exemplo. |
| **Strategy** | `ResumidorService` muda de comportamento conforme o `IChatClient` recebido. | Trocar o cliente troca a estratégia sem mudar o serviço. |
| **Factory** (simples) | `CriarCliente` escolhe e cria o cliente a partir de `PROVEDOR`. | É uma fábrica estática com `switch`, não o Factory Method com subclasses do GoF. |
| **Injeção de dependência** | `ResumidorService(IChatClient chat)` recebe a dependência pelo construtor. `Program.cs` é o *composition root*. | Permite testar com outro cliente. |
| **Fake** (dublê de teste) | `ClienteFalso` substitui o cliente real nos testes. | Não é padrão GoF, mas viabiliza testar sem rede nem chave. |

## 4. Fluxo de execução

1. Ler feeds; falhas de um feed são registradas e não interrompem a execução.
2. Manter itens das últimas 72h cujo link não está em `state/vistos.json`.
3. Pegar até `MAX_ITEMS_POR_EXECUCAO` itens, do mais recente ao mais antigo.
4. Para cada item, pedir ao modelo um texto e um SVG no formato `===POST===` / `===SVG===`.
5. Anexar a assinatura, gravar `.md` e `.svg` e marcar o link como visto.
6. Se a resposta do modelo estiver fora do formato, o item falha e os demais continuam.

## 5. Requisitos

**Funcionais**
- RF1: gerar rascunhos em português do Brasil.
- RF2: cada rascunho tem um SVG correspondente.
- RF3: não repetir uma novidade já processada.
- RF4: executar três vezes por dia.

**Não funcionais**
- RNF1: nenhuma publicação externa sem ação humana.
- RNF2: chaves de API vêm de variáveis de ambiente, nunca do código.
- RNF3: uma falha em uma fonte ou em um item não derruba a execução inteira.
- RNF4: o texto gerado não deve inventar números, datas ou recursos.

## 6. Decisões

| # | Decisão | Motivo |
|---|---|---|
| D1 | .NET 10, console app | Linguagem do público-alvo; sem dependências além de Microsoft.Extensions.AI no exemplo. |
| D2 | Anthropic como provedor de texto por enquanto | Escolha inicial. O provedor ainda está em aberto (ver seção 8). |
| D3 | Saída em SVG | Editável, leve e sem dependência de ferramenta de imagem. Exige conversão para PNG antes de publicar. |
| D4 | Agendamento pelo Agendador do Windows | Sem serviço em execução permanente. |
| D5 | Sem publicação automática | Evita posts com erro ou fora de tom. |

## 7. Riscos

- **Feeds mudam de endereço ou param.** Mitigação: aviso no log e revisão periódica de `feeds.json`.
- **Texto gerado com afirmações imprecisas.** Mitigação: revisão humana obrigatória antes de publicar.
- **Custo de API.** Mitigação: limite de itens por execução.
- **Animação do SVG não aparece no LinkedIn.** Mitigação: exportar PNG estático ou gerar vídeo/GIF, se necessário.

## 8. Questões em aberto

- [ ] Provedor de texto: Anthropic ou OpenAI (ou os dois).
- [ ] Endereço do GitHub, para a arte do post.
- [ ] Conteúdo da assinatura em `config/signature.txt`.
- [ ] Confirmar que as URLs de RSS em `config/feeds.json` estão ativas.
- [ ] Decidir se a publicação será manual ou via Posty.
