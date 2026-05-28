using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Plugins.Shared;

namespace Plugins.OpenFoodFacts;

public class OpenFoodFactsProvider : IMetadataProvider
{
    public string ProviderName => "OpenFoodFacts";

    // Ambiente di staging come da documentazione per progetti non-production
    private const string BaseUrl = "https://world.openfoodfacts.net";

    // Credenziali base64 "off:off" richieste per le operazioni di WRITE,
    // incluse qui per completezza anche se il GET non le richiede obbligatoriamente
    private const string BasicAuthEncoded = "b2ZmOm9mZg==";

    // Margine conservativo rispetto al limite ufficiale di 15 req/min:
    // aspettiamo almeno 6 secondi tra una richiesta e l'altra (~10 req/min)
    private static readonly TimeSpan RateLimitDelay = TimeSpan.FromSeconds(6);

    // Timestamp dell'ultima richiesta effettuata, condiviso tra tutte le chiamate
    private static DateTime _lastRequestTime = DateTime.MinValue;

    // Lock per rendere thread-safe il controllo del rate limit
    private static readonly SemaphoreSlim _rateLimitLock = new SemaphoreSlim(1, 1);

    // HttpClient statico: una sola istanza per tutta la vita del plugin,
    // evita l'esaurimento dei socket (socket exhaustion)
    private static readonly HttpClient _httpClient = BuildHttpClient();


    // Cache dei metadati per evitare chiamate duplicate nello stesso ciclo di vita del provider
    private bool _alreadyFetched = false;
    private string? _cachedImageUrl;
    private string? _cachedName;
    private string? _cachedDescription;

    private static HttpClient BuildHttpClient()
    {
        var client = new HttpClient();

        // User-Agent obbligatorio come da policy di OpenFoodFacts
        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "GestioLan/1.0 (https://github.com/CookieMaker443/GestioLan-Server)"
        );

        // Header di autenticazione Basic (off:off in base64)
        // Non richiesto per i GET ma consigliato per identificarsi
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", BasicAuthEncoded);

        return client;
    }

    // Normalizza il barcode al formato EAN-13 (13 cifre) come richiesto dall'API OFF.
    // EAN-8 viene esteso con 5 zeri iniziali, altri formati più corti con zeri iniziali fino a 13.
    // Se il barcode è già >= 13 cifre viene restituito invariato.
    private static string NormalizeBarcode(string barcode)
    {
        // Rimuove spazi e caratteri non numerici eventualmente presenti
        var digits = new string(barcode.Where(char.IsDigit).ToArray());

        if (digits.Length >= 13)
            return digits;

        // EAN-8: 8 cifre → padding a 13 con 5 zeri a sinistra
        // Tutti gli altri formati corti: padding a 13
        return digits.PadLeft(13, '0');
    }

    // Applica il rate limiting: se la chiamata precedente è troppo recente,
    // aspetta il tempo necessario prima di procedere
    private static async Task WaitForRateLimitAsync()
    {
        await _rateLimitLock.WaitAsync();
        try
        {
            var elapsed = DateTime.UtcNow - _lastRequestTime;
            if (elapsed < RateLimitDelay)
            {
                await Task.Delay(RateLimitDelay - elapsed);
            }
            _lastRequestTime = DateTime.UtcNow;
        }
        finally
        {
            _rateLimitLock.Release();
        }
    }

    // Scarica e mette in cache nome, URL immagine e descrizione nutrizionale del prodotto.
    // I campi assenti nel JSON vengono semplicemente omessi senza lanciare eccezioni.

    private async Task FetchMetadataAsync(string searchKey)
    {
        if (_alreadyFetched)
            return;

        var barcode = NormalizeBarcode(searchKey);
        var url = $"{BaseUrl}/api/v2/product/{barcode}.json" +
                  "?fields=product_name,image_front_url,nutriments";

        await WaitForRateLimitAsync();

        using var response = await _httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            _alreadyFetched = true;
            return;
        }

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // status == 0 → prodotto non trovato
        if (root.TryGetProperty("status", out var status) && status.GetInt32() == 0)
        {
            _alreadyFetched = true;
            return;
        }

        if (!root.TryGetProperty("product", out var product))
        {
            _alreadyFetched = true;
            return;
        }

        // Nome ufficiale del prodotto
        if (product.TryGetProperty("product_name", out var nameProp))
            _cachedName = nameProp.GetString();

        // URL immagine frontale
        if (product.TryGetProperty("image_front_url", out var imgProp))
            _cachedImageUrl = imgProp.GetString();

        // Costruzione della descrizione nutrizionale (valori per 100 g)
        if (product.TryGetProperty("nutriments", out var nutriments))
            _cachedDescription = BuildNutritionDescription(nutriments);

        _alreadyFetched = true;
    }

    // Costruisce una descrizione nutrizionale compatta da includere nel DB.
    // Omette le righe per i nutrienti assenti nel JSON.
    // Tronca a 250 caratteri per rispettare il limite della colonna.
    private static string BuildNutritionDescription(JsonElement nutriments)
    {
        var sb = new StringBuilder();

        // Mappa: chiave JSON OFF → etichetta leggibile
        var fields = new (string Key, string Label)[]
        {
            ("energy-kcal_100g",          "Kcal"),
            ("proteins_100g",             "Proteine"),
            ("carbohydrates_100g",        "Carboidrati"),
            ("sugars_100g",               "  di cui zuccheri"),
            ("fat_100g",                  "Grassi"),
            ("saturated-fat_100g",        "  di cui saturi"),
            ("fiber_100g",                "Fibre"),
        };

        foreach (var (key, label) in fields)
        {
            if (!nutriments.TryGetProperty(key, out var prop))
                continue;

            // I valori possono essere double o stringhe numeriche
            double? value = prop.ValueKind == JsonValueKind.Number
                ? prop.GetDouble()
                : double.TryParse(prop.GetString(), out var parsed) ? parsed : null;

            if (value is null)
                continue;

            // Kcal senza unità di misura, tutto il resto in grammi
            var unit = key.StartsWith("energy") ? "" : "g";
            var line = $"{label}: {value:0.#}{unit}\n";

            // Controlla che aggiungere questa riga non sfori il limite
            if (sb.Length + line.Length > 250)
                break;

            sb.Append(line);
        }

        return sb.ToString().TrimEnd();
    }


    public async Task<ProviderImageResult?> DownloadImageAsync(string searchKey)
    {
        await FetchMetadataAsync(searchKey);

        if (string.IsNullOrEmpty(_cachedImageUrl))
            return null;

        // Seconda richiesta per scaricare il binario dell'immagine,
        // anch'essa soggetta al rate limit
        await WaitForRateLimitAsync();

        var imageResponse = await _httpClient.GetAsync(_cachedImageUrl);
        if (!imageResponse.IsSuccessStatusCode)
            return null;

        // Ricava l'estensione dall'URL ignorando eventuali query string
        var ext = Path.GetExtension(_cachedImageUrl.Split('?')[0]);
        if (string.IsNullOrEmpty(ext))
            ext = ".jpg";

        // Copia in MemoryStream per chiudere subito la connessione HTTP
        // e restituire uno stream autonomo al chiamante
        var ms = new MemoryStream();
        await imageResponse.Content.CopyToAsync(ms);
        ms.Position = 0;

        return new ProviderImageResult
        {
            ImageStream = ms,
            SuggestedExtension = ext
        };
    }


    public async Task<string> GetCorrectNameAsync(string searchKey)
    {
        await FetchMetadataAsync(searchKey);
        return _cachedName ?? string.Empty;
    }

    public async Task<string> GetCorrectDescriptionAsync(string searchKey)
    {
        await FetchMetadataAsync(searchKey);
        return _cachedDescription ?? string.Empty;
    }
}