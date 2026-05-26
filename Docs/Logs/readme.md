# Sistema di Logging — GestioLAN API

## Indice

- [Panoramica](#panoramica)
- [Dipendenze NuGet](#dipendenze-nuget)
- [Configurazione Serilog in Program.cs](#configurazione-serilog-in-programcs)
- [CurrentUserService](#currentuserservice)
- [LogEnricherMiddleware](#logenrichermiddleware)
- [Ordine della pipeline in Program.cs](#ordine-della-pipeline-in-programcs)
- [Utilizzo nei Controller e nei Servizi](#utilizzo-nei-controller-e-nei-servizi)
- [Formato dell'output](#formato-delloutput)
- [Casi speciali](#casi-speciali)
- [Seq — interfaccia web per i log](#seq--interfaccia-web-per-i-log)
- [Configurazione appsettings.json](#configurazione-appsettingsjson)

---

## Panoramica

Il sistema di logging è basato su **Serilog** e si compone di tre parti principali:

```
Richiesta HTTP
    ↓
LogEnricherMiddleware       ← legge utente (JWT) + controller + action e li pusha nel LogContext
    ↓
Controller                  ← usa _logger.LogInformation(...) — ha già User/Service/Action
    ↓
Servizio                    ← usa _logger.LogInformation(...) — eredita User/Service/Action
    ↓
Serilog
    ├── Console             (output colorato nel terminale / Docker logs)
    └── File .txt           (su disco, un file per giorno, path da appsettings)
```

Ogni riga di log prodotta da qualsiasi punto dell'applicazione — controller, servizio, plugin — contiene automaticamente:

| Campo | Fonte | Esempio |
|---|---|---|
| `Timestamp` | Serilog | `2026-05-25 15:10:01` |
| `Level` | Serilog | `INFO`, `WARN`, `ERRO` |
| `User` | LogEnricherMiddleware → JWT | `mario.rossi` / `anonymous` |
| `Service` | LogEnricherMiddleware → routing | `Images` |
| `Action` | LogEnricherMiddleware → routing | `GetImageByName` |
| `Message` | chiamata `_logger.LogXxx(...)` | `Immagine servita: scheda.jpg` |

---

## Dipendenze NuGet

```xml
<!-- Nel .csproj -->
<PackageReference Include="Serilog.AspNetCore" Version="*" />
<PackageReference Include="Serilog.Sinks.File" Version="*" />
<PackageReference Include="Serilog.Sinks.Console" Version="*" />

<!-- Opzionale — interfaccia web per i log -->
<PackageReference Include="Serilog.Sinks.Seq" Version="*" />
```

Oppure via CLI:

```bash
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.File
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Sinks.Seq   # opzionale
```

---

## Configurazione Serilog in Program.cs

```csharp
// Legge il percorso del file di log dall'appsettings
string logsFolder = builder.Configuration["Storage:LogsPath"];

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()         // logga solo Info, Warning, Error, Fatal — esclude Debug e Verbose
    .Enrich.FromLogContext()            // NECESSARIO: permette a LogContext.PushProperty(...) di funzionare
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:yyyy-MM-dd HH:mm:ss}][{Level:u4}][{User}][{Service}][{Action}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(logsFolder,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss}][{Level:u4}][{User}][{Service}][{Action}] {Message:lj}{NewLine}{Exception}",
        rollingInterval: RollingInterval.Day,   // crea un nuovo file ogni giorno
        retainedFileCountLimit: 7)              // mantiene solo gli ultimi 7 giorni
    // .WriteTo.Seq("http://seq:5341")          // opzionale: interfaccia web
    .CreateLogger();

builder.Host.UseSerilog(); // sostituisce il sistema di logging di default di ASP.NET con Serilog
```

### Spiegazione dei parametri del template

| Token | Significato |
|---|---|
| `{Timestamp:yyyy-MM-dd HH:mm:ss}` | Data e ora della riga di log |
| `{Level:u4}` | Livello in maiuscolo, 4 caratteri (`INFO`, `WARN`, `ERRO`, `FATL`) |
| `{User}` | Proprietà custom pushata dal middleware — username dal JWT |
| `{Service}` | Proprietà custom pushata dal middleware — nome del controller |
| `{Action}` | Proprietà custom pushata dal middleware — nome del metodo |
| `{Message:lj}` | Testo del log (`:lj` = literal JSON, le stringhe non vengono quotate) |
| `{NewLine}` | Ritorno a capo |
| `{Exception}` | Stack trace completo se il log include un'eccezione |

### Livelli di log disponibili

```csharp
_logger.LogTrace(...)       // più dettagliato — di solito escluso in produzione
_logger.LogDebug(...)       // dettagli di debug — di solito escluso in produzione
_logger.LogInformation(...) // flusso normale dell'applicazione
_logger.LogWarning(...)     // qualcosa di inatteso ma non bloccante
_logger.LogError(...)       // errore, operazione fallita
_logger.LogCritical(...)    // errore grave, l'app potrebbe non continuare
```

Con `MinimumLevel.Information()` vengono scritti solo `Information` e superiori.

---

## CurrentUserService

**Percorso:** `src/GestioLan.API/Utils/Helpers/CurrentUserService.cs`

Legge lo username dell'utente autenticato dal token JWT tramite `IHttpContextAccessor`. Se non c'è un token valido (es. endpoint pubblico come Register/Login) restituisce `"anonymous"`.

```csharp
// Interfaccia
public interface ICurrentUserService
{
    string Username { get; }
    bool IsAdmin { get; }
}

// Implementazione
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    // Cerca prima il claim "username", poi ClaimTypes.Name, poi torna "anonymous"
    public string Username =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue("username")
        ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Name)
        ?? "anonymous";

    public bool IsAdmin =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue("isAdmin") == "true";
}
```

Registrazione in `Program.cs`:

```csharp
builder.Services.AddHttpContextAccessor();                          // rende disponibile IHttpContextAccessor
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>(); // registra il servizio
```

---

## LogEnricherMiddleware

**Percorso:** `src/GestioLan.API/Utils/Helpers/LogEnricherMiddleware.cs`

Middleware che viene eseguito ad ogni richiesta HTTP. Legge il controller e l'action dall'endpoint corrente, legge lo username dal JWT tramite `ICurrentUserService`, e li pusha nel `LogContext` di Serilog per tutta la durata della richiesta.

```csharp
public class LogEnricherMiddleware
{
    private readonly RequestDelegate _next;

    public LogEnricherMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ICurrentUserService currentUserService)
    {
        // Recupera l'endpoint risolto dal routing (disponibile perché siamo dopo UseRouting)
        var endpoint = context.GetEndpoint();

        // ControllerActionDescriptor contiene il nome del controller e dell'action
        var descriptor = endpoint?.Metadata
            .GetMetadata<Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor>();

        string controllerName = descriptor?.ControllerName ?? "System";  // es. "Images"
        string actionName = descriptor?.ActionName ?? "Unknown";          // es. "GetImageByName"

        // PushProperty aggiunge le proprietà al "contesto" di Serilog per questa richiesta.
        // Tutto il codice eseguito dentro _next(context) — controller, servizi, repository —
        // erediterà automaticamente queste proprietà in ogni riga di log.
        using (LogContext.PushProperty("User", currentUserService.Username))
        using (LogContext.PushProperty("Service", controllerName))
        using (LogContext.PushProperty("Action", actionName))
        {
            await _next(context); // passa la richiesta al controller
        }
        // Quando _next ritorna, il using fa il Dispose e le proprietà vengono rimosse
    }
}
```

### Perché il middleware e non un Filter?

Il middleware è preferibile perché:
- Intercetta **tutte** le richieste, incluse quelle che falliscono prima del controller (JWT non valido, route non trovata, ecc.)
- È più semplice — non richiede registrazione separata in `AddControllers()`
- Il `LogContext` funziona naturalmente con lo scope della richiesta

---

## Ordine della pipeline in Program.cs

L'ordine dei middleware è fondamentale. Il `LogEnricherMiddleware` deve stare **dopo** `UseAuthentication` e `UseAuthorization` perché ha bisogno che il JWT sia già stato letto e che l'endpoint sia già stato risolto dal routing.

```csharp
app.UseStaticFiles();
app.UseHttpsRedirection();
app.UseAuthentication();              // ← legge il JWT e popola HttpContext.User
app.UseAuthorization();               // ← valida i permessi ([Authorize], policy, ecc.)
app.UseMiddleware<LogEnricherMiddleware>(); // ← ha già User dal JWT + endpoint risolto
app.MapControllers();                 // ← non è un middleware, registra solo le route
```

Se il middleware venisse messo prima di `UseAuthentication`, `currentUserService.Username` tornerebbe sempre `"anonymous"` perché il JWT non è ancora stato processato.

---

## Utilizzo nei Controller e nei Servizi

### Iniezione di ILogger

Ogni controller e ogni servizio che vuole loggare inietta `ILogger<T>` dove `T` è la propria classe. ASP.NET lo fornisce automaticamente senza registrazioni aggiuntive.

```csharp
// Controller
public class ImagesController : ControllerBase
{
    private readonly IImageService _imageService;
    private readonly ILogger<ImagesController> _logger;

    public ImagesController(IImageService imageService, ILogger<ImagesController> logger)
    {
        _imageService = imageService;
        _logger = logger;
    }
}

// Servizio
public class ImageService : IImageService
{
    private readonly GestioLanContext _context;
    private readonly ILogger<ImageService> _logger;

    public ImageService(GestioLanContext context, IConfiguration config, ILogger<ImageService> logger)
    {
        _context = context;
        _logger = logger;
    }
}
```

La `T` serve a Serilog per popolare `{SourceContext}` — il nome completo della classe da cui proviene il log. Non influenza `User`, `Service` o `Action`.

### Cosa logga il controller vs il servizio

**Controller** — logga ciò che riguarda la richiesta HTTP: parametri ricevuti, risposta restituita, errori catturati nel catch.

**Servizio** — logga ciò che riguarda la logica di business: file salvati, record creati/modificati, file non trovati su disco.

```csharp
// Controller
[HttpPost("CreateImage")]
public async Task<IActionResult> CreateImage(string? itemName, IFormFile file)
{
    _logger.LogInformation("Richiesta creazione immagine per item: {ItemName}", itemName ?? "unknown");
    try
    {
        var message = await _imageService.CreateImageAsync(itemName, file);
        return Ok(new { message });
    }
    catch (ArgumentException ex)
    {
        _logger.LogWarning("Parametri non validi: {Error}", ex.Message);
        return BadRequest(ex.Message);
    }
}

// Servizio
public async Task<string> CreateImageAsync(string? itemName, IFormFile file)
{
    if (file == null || file.Length == 0)
    {
        _logger.LogWarning("File nullo o vuoto");
        throw new ArgumentException("Nessun file selezionato.");
    }

    string fileName = await UploadImage(_itemsFolder, file, itemName ?? "unknown");
    _logger.LogInformation("File salvato su disco: {FileName}", fileName);

    _context.Images.Add(new Image { FileName = fileName });
    await _context.SaveChangesAsync();
    _logger.LogInformation("Record creato nel DB con ID: {IdImage}", newImage.IdImage);

    return $"Caricata: {fileName}";
}
```

### Nessun LogContext manuale nei servizi

I servizi **non devono** chiamare `LogContext.PushProperty(...)`. Le proprietà `User`, `Service` e `Action` sono già state pushate dal middleware e sono disponibili per tutta la durata della richiesta, inclusi tutti i servizi chiamati a cascata.

---

## Formato dell'output

Esempio di sequenza di log per una singola chiamata `POST /api/images/CreateImage` da parte di `mario.rossi`:

```
[2026-05-25 15:10:01][INFO][mario.rossi][Images][CreateImage] Richiesta creazione immagine per item: scheda_rete
[2026-05-25 15:10:01][INFO][mario.rossi][Images][CreateImage] File salvato su disco: aB3xKq1z_scheda_rete.jpg
[2026-05-25 15:10:01][INFO][mario.rossi][Images][CreateImage] Record creato nel DB con ID: 42
```

Esempio con errore:

```
[2026-05-25 15:11:03][INFO][mario.rossi][Images][CreateImage] Richiesta creazione immagine per item: scheda_rete
[2026-05-25 15:11:03][WARN][mario.rossi][Images][CreateImage] File nullo o vuoto
[2026-05-25 15:11:03][WARN][mario.rossi][Images][CreateImage] Parametri non validi: Nessun file selezionato.
```

---

## Casi speciali

### Endpoint pubblici (Register, Login)

Gli endpoint senza `[Authorize]` non hanno un JWT nel token, quindi `currentUserService.Username` torna `"anonymous"`. Il middleware funziona ugualmente:

```
[2026-05-25 15:05:00][INFO][anonymous][Auth][Register] Nuovo utente registrato: mario.rossi
[2026-05-25 15:05:30][INFO][anonymous][Auth][Login] Login riuscito per: mario.rossi
```

### Plugin e background job

I plugin chiamano i servizi direttamente, senza passare per un controller HTTP — quindi non c'è un middleware che pusha il contesto. In questi casi è il metodo stesso a pushare manualmente:
l'importante è che i plugin abbiano dei logger, e che scrivino il loro nome identificativo

```csharp
public async Task<int> SaveImageFromStreamAsync(Stream imageStream, string ext, string? itemName)
{
    // Nessun controller sopra → pushiamo manualmente
    using (LogContext.PushProperty("User", "plugin"))
    using (LogContext.PushProperty("Service", "ImageService"))
    using (LogContext.PushProperty("Action", "SaveImageFromStream"))
    {
        _logger.LogInformation("Salvataggio immagine da plugin: {ItemName}", itemName ?? "unknown");
        // ...
    }
}
```

Output:
```
[2026-05-25 15:15:00][INFO][plugin][ImageService][SaveImageFromStream] Salvataggio immagine da plugin: cisco_switch
```

### Controller AI

Per il controller AI il middleware funziona come per tutti gli altri. In aggiunta conviene inserire "[AI]" nelmessaggio o nel mcp sever e identificare i tool usati
>Ancora Work in progress, da definire bene quando si inizia il server mcp

```csharp
// Nella risposta dell'LLM, i blocchi "tool_use" indicano quale tool MCP è stato chiamato
if (blockType == "tool_use" || blockType == "mcp_tool_use")
{
    _logger.LogInformation(
        "Tool MCP chiamato → {ToolName} | Input: {ToolInput}",
        toolName, toolInput);
}
```

Output:
```
[2026-05-25 15:20:00][INFO][mario.rossi][Ai][SendPrompt] Prompt ricevuto: trova tutti gli switch...
[2026-05-25 15:20:02][INFO][mario.rossi][Ai][SendPrompt] Tool MCP chiamato → query_items | Input: {"category":"switch"}
[2026-05-25 15:20:02][INFO][mario.rossi][Ai][SendPrompt] Risultato MCP ricevuto: [{"id":12,"name":"Cisco SG350"}...]
[2026-05-25 15:20:03][INFO][mario.rossi][Ai][SendPrompt] Risposta finale restituita all'utente
```

---

## Seq — interfaccia web per i log

Seq è un'applicazione web self-hostabile che riceve i log da Serilog e li mostra in un'interfaccia filtrabile per utente, livello, servizio, ecc. È opzionale ma utile in produzione.

### docker-compose.yml

```yaml
  seq:
    image: datalust/seq:latest
    container_name: GestioLan.Logs
    restart: always
    environment:
      - ACCEPT_EULA=Y
      - SEQ_FIRSTRUN_ADMINPASSWORD=${SEQ_FIRSTRUN_ADMINPASSWORD}
    ports:
      - "5341:80"   # interfaccia web → http://localhost:5341
    volumes:
      - ${RESOURCE_DEST_PATH}/seq_data:/data
    networks: 
      - gestionale_network q:/data  # cartella dedicata, separata dai log file
```

La cartella di Seq deve essere **separata** da quella dei log file testuali — Seq ci mette il suo database interno e gli indici.

### Abilitare il sink in Program.cs

```csharp
.WriteTo.Seq("http://seq:5341")
```
o meglio
```csharp
.WriteTo.Seq(builder.Configuration["ConnectionStrings:seq"] ?? "http://seq:5341")   // <-- Seq in Docker
```

cosi si assegna il link dell'interfaccia tramitevariabile di ambiente (per disaccoppiare i pezzi)
>NOTA: quando si assegna l'url di seq, bisogna esplicitare anche il protocollo(http://)
>quindi viene `http://[IP]:[PORTA]`

Il nome host `seq` funziona se l'API e Seq sono nella stessa rete Docker. Se li avvii separatamente usa l'IP o il nome del container.

---

## Configurazione appsettings.json

```json
{
  "Storage": {
    "UsersPath": "/app/data/uploads/users",
    "ItemsPath": "/app/data/uploads/items",
    "PluginsPath": "/app/plugins",
    "LogsPath": "/app/logs/api_log.txt"
  }
}
```

Il path di `LogsPath` viene letto in `Program.cs` e passato a `WriteTo.File(...)`. Serilog aggiunge automaticamente il suffisso della data quando `rollingInterval` è attivo — il file reale sarà ad esempio `api_log20260525.txt`.

### Bind mount nel docker-compose.yml

```yaml
gestiolanapi:
  volumes:
    - /percorso/reale/sul/server/logs:/app/logs   # log file testuali
    - /percorso/reale/sul/server/seq:/data        # dati Seq (se usato)
```

La cartella `/app/logs` sul container corrisponde al percorso reale sul server a sinistra dei due punti. Se non esiste Docker la crea al primo avvio, ma è meglio crearla a mano per evitare problemi di permessi:

```bash
mkdir -p /percorso/reale/sul/server/logs
```