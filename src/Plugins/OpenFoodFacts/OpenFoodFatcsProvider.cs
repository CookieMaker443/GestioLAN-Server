using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
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

    public async Task<ProviderImageResult?> DownloadImageAsync(string searchKey)
    {
        // Recupera solo il campo immagine frontale per minimizzare il payload
        var productUrl = $"{BaseUrl}/api/v2/product/{searchKey}.json?fields=image_front_url";

        await WaitForRateLimitAsync();

        using var metaResponse = await _httpClient.GetAsync(productUrl);
        if (!metaResponse.IsSuccessStatusCode)
            return null;

        var json = await metaResponse.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        // status == 0 significa prodotto non trovato nell'API di OFF
        if (doc.RootElement.TryGetProperty("status", out var status) && status.GetInt32() == 0)
            return null;

        if (!doc.RootElement.TryGetProperty("product", out var product))
            return null;

        if (!product.TryGetProperty("image_front_url", out var imageUrlProp))
            return null;

        var imageUrl = imageUrlProp.GetString();
        if (string.IsNullOrEmpty(imageUrl))
            return null;

        // Seconda richiesta per scaricare il binario dell'immagine,
        // anch'essa soggetta al rate limit
        await WaitForRateLimitAsync();

        var imageResponse = await _httpClient.GetAsync(imageUrl);
        if (!imageResponse.IsSuccessStatusCode)
            return null;

        // Ricava l'estensione dall'URL ignorando eventuali query string
        var ext = Path.GetExtension(imageUrl.Split('?')[0]);
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
}