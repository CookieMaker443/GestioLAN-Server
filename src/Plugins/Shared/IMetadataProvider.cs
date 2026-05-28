using System.IO;
using System.Threading.Tasks;

namespace Plugins.Shared;

public interface IMetadataProvider
{
    // Il nome univoco del provider (es. "OpenFoodFacts" o "FritzingAPI")
    // Deve combaciare al millimetro con quello che scriverai nella colonna "AssociatedProviderName" del DB
    string ProviderName { get; }

    // Riceve una chiave di ricerca (Barcode, seriale ecc.) e restituisce lo Stream dell'immagine scaricata
    Task<ProviderImageResult?> DownloadImageAsync(string searchKey);

    // Riceve il nome piu formale dell item 
    Task<string> GetCorrectNameAsync(string searchKey);

    // Riceve una breve descrizione deii item
    Task<string> GetCorrectDescriptionAsync(string sarchKey);
}

// Questa classe serve a impacchettare lo stream binario e l'estensione del file
public class ProviderImageResult
{
    public Stream ImageStream { get; set; } = null!;
    public string SuggestedExtension { get; set; } = null!; // es. ".jpg" o ".png"
}