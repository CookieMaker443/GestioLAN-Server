namespace GestioLan.API.Services.Metadata;

public interface IMetadataService
{
    // 1. Restituisce i nomi di tutti i plugin caricati in memoria
    IEnumerable<string> GetLoadedProviders();

    // 2. Associa un provider a una categoria nel DB
    Task AssociateProviderToCategoryAsync(int idCategory, string providerName);

    // 3. Scarica l'immagine tramite il plugin giusto e la salva via ImageService
    //    Restituisce l'IdImage salvato, oppure null se nessun provider è configurato
    Task<int?> FetchAndSaveImageAsync(string searchKey, int? idCategory, string? itemName = null);
}