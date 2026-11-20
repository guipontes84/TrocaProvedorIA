using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;

// Rascunhos de posts do LinkedIn (IA x C#), gerados a partir de feeds de novidades.
// Execução típica: 3x por dia pelo Agendador de Tarefas (ver scripts/agendar-tarefas.ps1).

var baseDir = AppContext.BaseDirectory;
var root = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..")); // pasta do projeto em dev
if (!File.Exists(Path.Combine(root, "LinkedinDicas.csproj")))
    root = Directory.GetCurrentDirectory();

var configDir = Path.Combine(root, "config");
var stateDir = Path.Combine(root, "state");
var draftsDir = Path.Combine(root, "drafts");
Directory.CreateDirectory(stateDir);
Directory.CreateDirectory(draftsDir);

var apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
var model = Environment.GetEnvironmentVariable("ANTHROPIC_MODEL") ?? "claude-sonnet-5-5";
var maxItems = int.TryParse(Environment.GetEnvironmentVariable("MAX_ITEMS_POR_EXECUCAO"), out var m) ? m : 3;

if (string.IsNullOrWhiteSpace(apiKey))
{
    Console.Error.WriteLine("Defina a variável de ambiente ANTHROPIC_API_KEY.");
    return 1;
}

var signature = ReadOptional(Path.Combine(configDir, "signature.txt"));
var feeds = JsonSerializer.Deserialize<List<Feed>>(File.ReadAllText(Path.Combine(configDir, "feeds.json")),
    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
var seenPath = Path.Combine(stateDir, "vistos.json");
var seen = File.Exists(seenPath)
    ? JsonSerializer.Deserialize<HashSet<string>>(File.ReadAllText(seenPath)) ?? []
    : new HashSet<string>();

using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
http.DefaultRequestHeaders.UserAgent.ParseAdd("LinkedinDicas/1.0");

// 1) Coleta de itens dos feeds
var items = new List<NewsItem>();
foreach (var feed in feeds)
{
    try
    {
        var xml = await http.GetStringAsync(feed.Url);
        items.AddRange(ParseFeed(feed, xml));
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[aviso] Falha no feed {feed.Name}: {ex.Message}");
    }
}

// 2) Filtra novidades das últimas 72h que ainda não foram usadas
var cutoff = DateTimeOffset.UtcNow.AddHours(-72);
var novidades = items
    .Where(i => i.Published >= cutoff && !seen.Contains(i.Link))
    .OrderByDescending(i => i.Published)
    .Take(maxItems)
    .ToList();

if (novidades.Count == 0)
{
    Console.WriteLine("Nenhuma novidade nova encontrada.");
    return 0;
}

// 3) Gera um rascunho por novidade
var stamp = DateTime.Now.ToString("yyyy-MM-dd_HHmm");
foreach (var item in novidades)
{
    try
    {
        var (post, svg) = await GerarPostAsync(http, apiKey, model, item);
        if (!string.IsNullOrWhiteSpace(signature))
            post = post.TrimEnd() + "\n\n" + signature.Trim();

        var baseName = $"{stamp}_{Slug(item.Title)}";
        var fileName = $"{baseName}.md";
        var svgName = $"{baseName}.svg";
        var content = $"<!-- Fonte: {item.Link} -->\n<!-- Feed: {item.Source} | Publicado: {item.Published:yyyy-MM-dd HH:mm} UTC -->\n<!-- Imagem: {svgName} (exporte para PNG antes de publicar) -->\n\n{post}\n";
        File.WriteAllText(Path.Combine(draftsDir, fileName), content, Encoding.UTF8);
        File.WriteAllText(Path.Combine(draftsDir, svgName), svg, Encoding.UTF8);

        seen.Add(item.Link);
        Console.WriteLine($"[ok] {fileName}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[erro] {item.Title}: {ex.Message}");
    }
}

File.WriteAllText(seenPath, JsonSerializer.Serialize(seen, new JsonSerializerOptions { WriteIndented = true }));
return 0;

// ---------- Helpers ----------

static string ReadOptional(string path) => File.Exists(path) ? File.ReadAllText(path) : "";

static IEnumerable<NewsItem> ParseFeed(Feed feed, string xml)
{
    var doc = XDocument.Parse(xml);
    XNamespace atom = "http://www.w3.org/2005/Atom";

    // RSS 2.0
    foreach (var entry in doc.Descendants("item"))
    {
        yield return new NewsItem(
            feed.Name,
            entry.Element("title")?.Value.Trim() ?? "",
            entry.Element("link")?.Value.Trim() ?? "",
            entry.Element("description")?.Value.Trim() ?? "",
            ParseDate(entry.Element("pubDate")?.Value));
    }

    // Atom
    foreach (var entry in doc.Descendants(atom + "entry"))
    {
        var link = entry.Elements(atom + "link").FirstOrDefault(l => (string?)l.Attribute("rel") is null or "alternate")
                          ?.Attribute("href")?.Value ?? "";
        yield return new NewsItem(
            feed.Name,
            entry.Element(atom + "title")?.Value.Trim() ?? "",
            link,
            entry.Element(atom + "summary")?.Value.Trim() ?? "",
            ParseDate(entry.Element(atom + "updated")?.Value ?? entry.Element(atom + "published")?.Value));
    }
}

static DateTimeOffset ParseDate(string? value) =>
    DateTimeOffset.TryParse(value, out var d) ? d : DateTimeOffset.MinValue;

static async Task<(string Post, string Svg)> GerarPostAsync(HttpClient http, string apiKey, string model, NewsItem item)
{
    const string system = """
        Você escreve posts para o LinkedIn em português do Brasil, para um desenvolvedor .NET/C#
        que quer mostrar como IA se aplica ao ecossistema C#.

        Regras de estilo:
        - Abertura curta e direta, com um gancho que explique por que a novidade importa.
        - Texto em parágrafos curtos, com lista de até 5 itens usando emojis de marcação (🔹 ou ✅).
        - Se a novidade permitir, inclua uma comparação simples (ex.: Anthropic x OpenAI, RAG x fine-tuning,
          Skills x prompts soltos) ou um exemplo prático em C# (até 10 linhas).
        - Explique o ângulo "IA x C#": como isso se aplica a um projeto .NET (Microsoft.Extensions.AI,
          Microsoft Agent Framework, Semantic Kernel, ASP.NET Core etc.), sem inventar APIs.
        - Quando fizer sentido, traga uma DICA prática e concreta sobre uma destas áreas:
          Arquitetura de Software (Clean Architecture, DDD, CQRS, SOLID); APIs REST e Microserviços;
          Mensageria (Kafka, RabbitMQ, Amazon SQS, Azure Service Bus); Cloud & DevOps (AWS, Docker,
          Kubernetes, Azure DevOps, Jenkins, SonarQube); Bancos SQL e NoSQL (SQL Server, Oracle,
          PostgreSQL, MongoDB, Redis); Front-end (Angular, React, Vue.js, TypeScript).
        - Termine com uma pergunta ou convite para troca de ideias.
        - Entre 800 e 1.200 caracteres no corpo, seguido de 5 a 8 hashtags (#dotnet #csharp #ia ...).
        - Não invente números, datas ou recursos. Se faltar informação no resumo, diga o que é conhecido
          e não especule.
        - Formato da resposta: primeiro a linha ===POST=== seguida do texto do post; depois a linha ===SVG===
          seguida de UM bloco <svg> autocontido (viewBox="0 0 1200 627", fundo claro, texto em português,
          no máximo 8 elementos) que ilustra visualmente a explicação principal (fluxo, comparação ou diagrama).
          Não escreva nada fora desses dois blocos.
        """;

    var userMsg = $"""
        Novidade: {item.Title}
        Fonte: {item.Source}
        Link: {item.Link}
        Resumo do feed:
        {item.Summary}
        """;

    var body = new JsonObject
    {
        ["model"] = model,
        ["max_tokens"] = 1200,
        ["system"] = system,
        ["messages"] = new JsonArray
        {
            new JsonObject { ["role"] = "user", ["content"] = userMsg }
        }
    };

    using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages")
    {
        Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json")
    };
    req.Headers.Add("x-api-key", apiKey);
    req.Headers.Add("anthropic-version", "2023-06-01");

    using var resp = await http.SendAsync(req);
    var json = await resp.Content.ReadAsStringAsync();
    if (!resp.IsSuccessStatusCode)
        throw new InvalidOperationException($"API retornou {(int)resp.StatusCode}: {json}");

    using var doc = JsonDocument.Parse(json);
    var text = doc.RootElement.GetProperty("content")[0].GetProperty("text").GetString() ?? "";

    var iPost = text.IndexOf("===POST===", StringComparison.Ordinal);
    var iSvg = text.IndexOf("===SVG===", StringComparison.Ordinal);
    if (iPost < 0 || iSvg < 0 || iSvg < iPost)
        throw new InvalidOperationException("Resposta do modelo fora do formato esperado.");

    var post = text[(iPost + "===POST===".Length)..iSvg].Trim();
    var svg = text[(iSvg + "===SVG===".Length)..].Trim();
    // Remove cercas de código caso o modelo as tenha usado
    svg = svg.Replace("```svg", "").Replace("```xml", "").Replace("```", "").Trim();
    if (!svg.StartsWith("<svg", StringComparison.OrdinalIgnoreCase))
        throw new InvalidOperationException("SVG ausente na resposta do modelo.");

    return (post, svg);
}

static string Slug(string text)
{
    var chars = text.ToLowerInvariant()
        .Select(c => char.IsLetterOrDigit(c) ? c : '-')
        .ToArray();
    var s = new string(chars);
    while (s.Contains("--")) s = s.Replace("--", "-");
    return s.Trim('-')[..Math.Min(s.Trim('-').Length, 60)];
}

record Feed(string Name, string Url, string[] Topics);

record NewsItem(string Source, string Title, string Link, string Summary, DateTimeOffset Published);
