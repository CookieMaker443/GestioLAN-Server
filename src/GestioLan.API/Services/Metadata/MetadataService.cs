using GestioLan.API.Models;
using GestioLan.API.Services.Images;
using Microsoft.EntityFrameworkCore;
using Plugins.Shared;

namespace GestioLan.API.Services.Metadata;

public class MetadataService : IMetadataService
{
    private readonly IEnumerable<IMetadataProvider> _plugins;
    private readonly GestioLanContext _context;
    private readonly IImageService _imageService;
    private readonly ILogger<MetadataService> _logger;


    public MetadataService(
        IEnumerable<IMetadataProvider> plugins,
        GestioLanContext context,
        IImageService imageService,
        ILogger<MetadataService> logger)
    {
        _plugins = plugins;
        _context = context;
        _imageService = imageService;
        _logger = logger;
    }

    // 1. CENSIMENTO: restituisce i nomi di tutti i plugin presenti in memoria
    public IEnumerable<string> GetLoadedProviders()
    {
        return _plugins.Select(p => p.ProviderName);
    }

    // 2. ASSOCIAZIONE: salva nel DB quale plugin gestisce una determinata categoria
    public async Task AssociateProviderToCategoryAsync(int idCategory, string providerName)
    {
        // Sicurezza: verifica che il plugin richiesto esista davvero in memoria
        bool pluginExists = _plugins.Any(p =>
            p.ProviderName.Equals(providerName, StringComparison.OrdinalIgnoreCase));

        if (!pluginExists)
        {
            _logger.LogWarning(
                "[MetadataService] Tentativo di associare provider inesistente '{ProviderName}' alla categoria {IdCategory}",
                providerName, idCategory);

            throw new ArgumentException(
                $"Il plugin '{providerName}' non è caricato. Provider disponibili: {string.Join(", ", GetLoadedProviders())}");
        }

        var category = await _context.Categories.FindAsync(idCategory)
            ?? throw new KeyNotFoundException($"Categoria con id {idCategory} non trovata");

        category.AssociatedProviderName = providerName;
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "[MetadataService] Provider '{ProviderName}' associato alla categoria {IdCategory}",
            providerName, idCategory);
    }

    // 3. CORE: scarica l'immagine provando i provider delle categorie attive
    //    nell'ordine in cui sono nel DB, si ferma al primo risultato non-null
    public async Task<int?> FetchAndSaveImageAsync(string searchKey, int? idCategory, string? itemName = null)
    {
        if (idCategory == null)
        {
            _logger.LogDebug(
                "[MetadataService] Item '{SearchKey}' senza categoria, skip recupero immagine automatico",
                searchKey);
            return null;
        }
 
        // Recupera tutte le categorie che hanno un provider associato e il cui bit
        // è attivo nella bitmask dell'item (es. idCategory=0011 matcha sia 0001 che 0010)
        var activeCategories = await _context.Categories
            .Where(c => c.AssociatedProviderName != null &&
                        (idCategory.Value & c.IdCategory) == c.IdCategory)
            .ToListAsync();
 
        if (activeCategories.Count == 0)
        {
            _logger.LogDebug(
                "[MetadataService] Nessuna categoria con provider associato trovata per bitmask {IdCategory}",
                idCategory);
            return null;
        }
 
        _logger.LogInformation(
            "[MetadataService] Trovate {Count} categorie con provider per bitmask {IdCategory}: [{Providers}]",
            activeCategories.Count,
            idCategory,
            string.Join(", ", activeCategories.Select(c => c.AssociatedProviderName)));
 
        // Prova i provider uno alla volta — si ferma al primo che restituisce un'immagine
        foreach (var category in activeCategories)
        {
            var plugin = _plugins.FirstOrDefault(p =>
                p.ProviderName.Equals(category.AssociatedProviderName, StringComparison.OrdinalIgnoreCase));
 
            // DLL rimossa a mano: anomalia di configurazione, logga e passa al prossimo
            if (plugin == null)
            {
                _logger.LogError(
                    "[MetadataService] Il provider '{ProviderName}' è configurato nel DB per la categoria {IdCategory} " +
                    "ma la sua DLL non è presente in /plugins. Saltato.",
                    category.AssociatedProviderName, category.IdCategory);
                continue;
            }
 
            _logger.LogInformation(
                "[MetadataService] Tentativo download con '{ProviderName}' per searchKey '{SearchKey}'",
                plugin.ProviderName, searchKey);
 
            // Il plugin gestisce internamente eccezioni di rete e restituisce null in caso di fallimento
            var result = await plugin.DownloadImageAsync(searchKey);
 
            if (result == null)
            {
                _logger.LogInformation(
                    "[MetadataService] '{ProviderName}' non ha trovato nulla per '{SearchKey}', provo il prossimo provider",
                    plugin.ProviderName, searchKey);
                continue;
            }
 
            // Trovata — salva e restituisce subito senza provare gli altri
            int savedImageId = await _imageService.SaveImageFromStreamAsync(
                                result.ImageStream, 
                                itemName,               // <-- invece di searchKey
                                result.SuggestedExtension);
 
            _logger.LogInformation(
                "[MetadataService] Immagine salvata (id={ImageId}) tramite '{ProviderName}' per '{SearchKey}'",
                savedImageId, plugin.ProviderName, searchKey);
 
            // restituisce l'id ottenuto dalla tabella del db
            return savedImageId;
        }
 
        // Tutti i provider hanno restituito null
        _logger.LogInformation(
            "[MetadataService] Nessun provider ha trovato un'immagine per '{SearchKey}'",
            searchKey);
        return null;
    }
}