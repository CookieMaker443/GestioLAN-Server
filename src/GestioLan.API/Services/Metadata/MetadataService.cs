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
                "[MetadataService] Trying to link inexisting provider '{ProviderName}' to category {IdCategory}",
                providerName, idCategory);

            throw new ArgumentException(
                $"Plugin '{providerName}' not loaded. Provider avaiable: {string.Join(", ", GetLoadedProviders())}");
        }

        var category = await _context.Categories.FindAsync(idCategory)
            ?? throw new KeyNotFoundException($"Categoria con id {idCategory} non trovata");

        category.AssociatedProviderName = providerName;
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "[MetadataService] Provider '{ProviderName}' linked to category {IdCategory}",
            providerName, idCategory);
    }

    //risolve il plugin per una categoria (o null)
    // e logga eventuali anomalie di configurazione
    private IMetadataProvider? ResolvePlugin(
        string? providerName,
        int categoryId)
    {
        if (providerName == null)
            return null;

        var plugin = _plugins.FirstOrDefault(p =>
            p.ProviderName.Equals(providerName, StringComparison.OrdinalIgnoreCase));

        // DLL rimossa a mano: anomalia di configurazione, logga e passa al prossimo
        if (plugin == null)
        {
            _logger.LogError(
                "[MetadataService] The provider '{ProviderName}' is configured in the DB for the category {IdCategory} " +
                "but the DLL does not exist in /plugins. Skipped.",
                providerName, categoryId);
        }

        return plugin;
    }

    // carica le categorie attive per la bitmask
    private async Task<List<Category>> GetActiveCategoriesAsync(int idCategory)
    {
        return await _context.Categories
            .Where(c => c.AssociatedProviderName != null &&
                        (idCategory & c.IdCategory) == c.IdCategory)
            .ToListAsync();
    }

    // 3. CORE: scarica l'immagine provando i provider delle categorie attive
    //    nell'ordine in cui sono nel DB, si ferma al primo risultato non-null
    public async Task<int?> FetchAndSaveImageAsync(string searchKey, int? idCategory, string? itemName = null)
    {
        if (idCategory == null)
        {
            _logger.LogDebug(
                "[MetadataService] Item '{SearchKey}' without category, skip fetching image",
                searchKey);
            return null;
        }
 
        // Recupera tutte le categorie che hanno un provider associato e il cui bit
        // è attivo nella bitmask dell'item (es. idCategory=0011 matcha sia 0001 che 0010)
        var activeCategories = await GetActiveCategoriesAsync(idCategory.Value);

        if (activeCategories.Count == 0)
        {
            _logger.LogDebug(
                "[MetadataService] No category with provider linked found for bitmask {IdCategory}",
                idCategory);
            return null;
        }
 
        _logger.LogInformation(
            "[MetadataService] Found {Count} category with provider or bitmask {IdCategory}: [{Providers}]",
            activeCategories.Count,
            idCategory,
            string.Join(", ", activeCategories.Select(c => c.AssociatedProviderName)));
 
        // Prova i provider uno alla volta — si ferma al primo che restituisce un'immagine
        foreach (var category in activeCategories)
        {
            var plugin = ResolvePlugin(category.AssociatedProviderName, category.IdCategory);
 
            _logger.LogInformation(
                "[MetadataService] Trying to download with '{ProviderName}' using searchKey '{SearchKey}'",
                plugin.ProviderName, searchKey);
 
            // Il plugin gestisce internamente eccezioni di rete e restituisce null in caso di fallimento
            var result = await plugin.DownloadImageAsync(searchKey);
 
            if (result == null)
            {
                _logger.LogInformation(
                    "[MetadataService] '{ProviderName}' has found nothing for '{SearchKey}', trying next provider",
                    plugin.ProviderName, searchKey);
                continue;
            }
 
            // Trovata — salva e restituisce subito senza provare gli altri
            int savedImageId = await _imageService.SaveImageFromStreamAsync(
                                result.ImageStream, 
                                result.SuggestedExtension,
                                itemName);  // <-- invece di searchKey
 
            _logger.LogInformation(
                "[MetadataService] Image saved (id={ImageId}) with '{ProviderName}' for '{SearchKey}'",
                savedImageId, plugin.ProviderName, searchKey);
 
            // restituisce l'id ottenuto dalla tabella del db
            return savedImageId;
        }
 
        // Tutti i provider hanno restituito null
        _logger.LogInformation(
            "[MetadataService] No provider has found an image for '{SearchKey}'",
            searchKey);
        return null;
    }

    // 4. CORE — NOME
    // Recupera il nome "ufficiale" dal primo provider che restituisce una stringa non vuota
    public async Task<string?> FetchNameAsync(string searchKey, int? idCategory)
    {
        if (idCategory == null)
        {
            _logger.LogDebug(
                "[MetadataService] Item '{SearchKey}' without category, skipping auto name fetching",
                searchKey);
            return null;
        }

        var activeCategories = await GetActiveCategoriesAsync(idCategory.Value);

        if (activeCategories.Count == 0)
        {
            _logger.LogDebug(
                "[MetadataService] No category with provider linked found for bitmask {IdCategory} (name)",
                idCategory);
            return null;
        }

        foreach (var category in activeCategories)
        {
            var plugin = ResolvePlugin(category.AssociatedProviderName, category.IdCategory);
            if (plugin == null)
                continue;

            _logger.LogInformation(
                "[MetadataService] Trying to download with '{ProviderName}' using searchKey '{SearchKey}'",
                plugin.ProviderName, searchKey);

            var name = await plugin.GetCorrectNameAsync(searchKey);

            if (!string.IsNullOrWhiteSpace(name))
            {
                _logger.LogInformation(
                    "[MetadataService] Name '{Name}' found trough '{ProviderName}' for '{SearchKey}'",
                    name, plugin.ProviderName, searchKey);
                return name;
            }

            _logger.LogInformation(
                "[MetadataService] '{ProviderName}' has found nothing for '{SearchKey}', trying next provider",
                plugin.ProviderName, searchKey);
        }

        _logger.LogInformation(
            "[MetadataService] No provider has found a name for '{SearchKey}'",
            searchKey);
        return null;
    }

    // 5. CORE — DESCRIZIONE
    // Recupera la descrizione nutrizionale dal primo provider che restituisce una stringa non vuota
    public async Task<string?> FetchDescriptionAsync(string searchKey, int? idCategory)
    {
        if (idCategory == null)
        {
            _logger.LogDebug(
                "[MetadataService] Item '{SearchKey}' without category, skip fetching description",
                searchKey);
            return null;
        }

        var activeCategories = await GetActiveCategoriesAsync(idCategory.Value);

        if (activeCategories.Count == 0)
        {
            _logger.LogDebug(
                "[MetadataService] No category with provider linked found for bitmask {IdCategory}(description)",
                idCategory);
            return null;
        }

        foreach (var category in activeCategories)
        {
            var plugin = ResolvePlugin(category.AssociatedProviderName, category.IdCategory);
            if (plugin == null)
                continue;

            _logger.LogInformation(
                "[MetadataService] Trying fetching description with '{ProviderName}' using searchKey '{SearchKey}'",
                plugin.ProviderName, searchKey);

            var description = await plugin.GetCorrectDescriptionAsync(searchKey);

            if (!string.IsNullOrWhiteSpace(description))
            {
                _logger.LogInformation(
                    "[MetadataService] Description found trough '{ProviderName}' for '{SearchKey}'",
                    plugin.ProviderName, searchKey);
                return description;
            }

            _logger.LogInformation(
                "[MetadataService] '{ProviderName}' has found nothing for '{SearchKey}', trying next provider",
                plugin.ProviderName, searchKey);
        }

        _logger.LogInformation(
            "[MetadataService] No provider has found a description for '{SearchKey}'",
            searchKey);
        return null;
    }
}