# Architettura Plugin: IMetadataProvider

L'interfaccia `IMetadataProvider` definisce il **contratto architetturale** tra l'applicazione principale (`GestioLan.API`) e i moduli di terze parti o scraper esterni (i "Plugin").

Il suo scopo principale è astrarre la logica di recupero delle informazioni e delle immagini associate ai beni inventariati, standardizzando la comunicazione ed evitando l'accoppiamento rigido (*tight coupling*) tra il core del server e le API esterne (es. OpenFoodFacts, Fritzing, ecc.).

---

## Come Funziona il Ciclo di Vita

```
[Avvio Server]
       │
       ▼
Riflessione (Program.cs) ──> Scansiona la cartella /plugins ──> Carica le DLL in memoria
                                                                       │
[Richiesta Utente]                                                     ▼
Iniezione nel Service <── Registra automaticamente ogni classe come IMetadataProvider
       │
       ▼
MetadataService ──> Legge il 'AssociatedProviderName' dal DB
       │
       ▼
Esecuzione ──> Trova il plugin con quel nome ──> Esegue DownloadImageAsync()

```

---

## Specifiche del Codice (`IMetadataProvider.cs`)

Il file si trova in `Plugins.Shared` e contiene sia l'interfaccia di contratto che il DTO (*Data Transfer Object*) di ritorno per i flussi binari.

```csharp
using System.IO;
using System.Threading.Tasks;

namespace Plugins.Shared;

// Contratto fondamentale che ogni plugin di recupero metadati deve implementare.
public interface IMetadataProvider
{
    // Il nome identificativo e univoco del provider.
    string ProviderName { get; }

    // Scarica asincronamente un'immagine da un servizio esterno partendo da una chiave di ricerca.
    // searchKey Il codice a barre (EAN), seriale hardware o SKU del componente
    //Un oggetto ProviderImageResult contenente lo stream o null se non trovato
    Task<ProviderImageResult?> DownloadImageAsync(string searchKey);
    
    // Riceve il nome piu formale dell item 
    Task<string> GetCorrectNameAsync(string searchKey);

    // Riceve una breve descrizione deii item
    Task<string> GetCorrectDescriptionAsync(string sarchKey);
}

/// Modello di ritorno standardizzato per incapsulare i dati binari dell'immagine scaricata.
public class ProviderImageResult
{
    // Il flusso di byte aperto direttamente dalla risposta di rete.
    // Evita di allocare l'intera immagine in un array di byte in memoria.
    public Stream ImageStream { get; set; } = null!;
    //L'estensione del file comprensiva di punto iniziale (es. ".png", ".jpg", ".webp").
    //Viene dedotta dal Content-Type HTTP o dall'URL dell'immagine sorgente
    public string SuggestedExtension { get; set; } = null!;
}

```

---

## Analisi dei Membri e Razionale Progettuale

### 1. Proprietà `ProviderName`

* **Cosa fa:** Restituisce una stringa fissa (es. `return "OpenFoodFacts";`).
* **Perché esiste:** Permette al backend di mappare i plugin dinamicamente. Quando il `MetadataService` estrae una categoria dal database, legge il campo stringa `AssociatedProviderName`. Tramite questa proprietà, seleziona in memoria il plugin corretto senza dover conoscere a priori la classe concreta o il file `.dll` originale.
* **Vantaggio collaterale:** Fornisce un'etichetta parlante per i file di log di Serilog (es. `[Plugin Loader] Caricato con successo: OpenFoodFacts`).

### 2. Metodi 

`DownloadImageAsync`
* **Cosa fa:** Accetta una stringa (il codice di ricerca) ed esegue una chiamata di rete non bloccante verso l'endpoint esterno.
* **Perché usa `Task`:** Il recupero dei dati avviene via Internet. L'uso della programmazione asincrona (`async/await`) è vitale per liberare i thread del server web durante l'attesa della risposta I/O della rete, garantendo che l'API rimanga reattiva.
* **Perché restituisce `null` anziché lanciare un'eccezione:** Se un codice a barre non è presente nei server di OpenFoodFacts, non si tratta di un errore software (anomalia di codice), ma di un esito operativo plausibile. Restituire `null` permette all'applicazione principale di procedere salvando l'oggetto nell'inventario semplicemente senza immagine.

` GetCorrectNameAsync`
* **Cosa fa:** Prende la searchkey e prova a oottere il nome piu professionale:
```Example
mms -> M&M's
nutella -> Nutella
arduiiino -> Arduino Uno Q
```

`GetCorrectDescriptionAsync` si comporta alla stessa maniera di getNamema per la descrizione

### 3. Classe `ProviderImageResult`

* **Perché usa `Stream`:** Passare un oggetto `Stream` (specificatamente un `NetworkStream` o un `MemoryStream`) permette di fare *piping* dei dati. L'applicazione principale può agganciare questo flusso e scriverlo direttamente sul disco o su un cloud storage, riducendo l'impatto sulla memoria RAM del server, specialmente con immagini pesanti o richieste simultanee.
* **Perché serve `SuggestedExtension`:** I server esterni rispondono con formati diversi. `GestioLan.API` deve rinominare il file sul disco host usando la chiave di ricerca (es. `80022401.png`). Senza questa proprietà, l'API principale non saprebbe come battezzare il file, rischiando di salvare strutture PNG con estensioni `.jpg` false, corrompendo i file multimediali dell'applicazione.

---

## Regole di Isolamento (Cosa *NON* deve fare il Plugin)

Per mantenere l'architettura pulita, ogni plugin sviluppato deve attenersi a queste limitazioni:

1. **Nessuna scrittura su disco:** Il plugin non deve conoscere i percorsi di archiviazione dell'applicazione principale (`/app/data/uploads/...`). Si limita a scaricare e passare i dati grezzi.
2. **Nessun accesso al Database di GestioLAN:** Il plugin è agnostico; non sa cosa sia Entity Framework o MySQL. Riceve input semplici e restituisce output semplici.
3. **Gestione interna delle eccezioni di rete:** Se l'API esterna risponde con un errore `500` o va in timeout, il plugin deve catturare l'eccezione internamente, effettuare il logging e restituire `null`, impedendo al crash di propagarsi e far fallire l'endpoint principale di salvataggio dell'inventario.