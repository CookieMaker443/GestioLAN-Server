# Image & Metadata Provider Plugin System

## Overview

Questo modulo definisce un'architettura a plugin estensibile per il software, progettata per consentire l'integrazione di diversi provider di immagini e metadati tramite API esterne. Sfruttando la **Reflection** di .NET, il sistema scansiona e carica dinamicamente i plugin personalizzati all'avvio, automatizzando il fetching delle immagini in base alle categorie degli articoli e ai codici a barre.

## Requirements / Dependencies

* **.NET Core**.
* Modulo condiviso `shared.csproj` (contenente l'interfaccia `IMetadataProvider.cs`).

## Architecture

Il sistema si basa su un'architettura disaccoppiata in cui il core del software non conosce i dettagli implementativi delle singole API esterne.

* **`src/Plugins/shared.csproj`**: Un modulo indipendente che funge da contratto comune. Contiene le interfacce che ogni plugin deve implementare.
* **Plugin Custom (`.dll`)**: Librerie di classi separate che implementano l'interfaccia di core, compilate e inserite in una cartella specifica per essere caricate a runtime.

### Logica di Automazione (Bitmask & Categorie)

Il database gestisce l'associazione tra le categorie dei prodotti e i provider API tramite una **bitmask** (maschera di bit).

* Ogni categoria ha una colonna dedicata che specifica quale API provider è associato.
* Quando viene inserito un nuovo articolo nel sistema, il software verifica la bitmask della categoria assegnata e avvia automaticamente il processo di fetching dell'immagine dal provider corrispondente usando il codice a barre.

---

## API Reference / Usage

### Interfaccia `IMetadataProvider`

Tutti i plugin custom devono implementare questa interfaccia definita nel modulo `shared`.

#### `getImageName()`

* **Descrizione:** Definisce la logica di fetching verso l'API esterna utilizzando il codice a barre per recuperare l'immagine e restituisce il nome del file o la stringa identificativa dell'immagine recuperata.
* **Firma:** `string getImageName();`
* **Valore di ritorno:** `string` — Il nome dell'immagine o l'identificativo restituito dall'API del provider.

> ⚠️ Note: La firma del metodo indicata nei requisiti (`string getImageName();`) non accetta parametri in ingresso. Per poter effettuare il fetching tramite codice a barre, si assume che il barcode debba essere iniettato nel costruttore della classe che implementa il plugin, oppure che l'interfaccia reale preveda un parametro (es. `string getImageName(string barcode);`).

---

## Installation & Setup

1. **Riferimento all'interfaccia:** Fase di sviluppo.
Creare un nuovo progetto di libreria di classi in C#. Scaricare il pacchetto NuGet ufficiale del modulo `shared` (se disponibile) oppure includere direttamente il file sorgente `IMetadataProvider.cs` come dipendenza.


2. **Implementazione del codice:** Fase di sviluppo.
Creare una classe pubblica che implementi `IMetadataProvider` e sviluppare la logica di chiamata HTTP verso l'API del provider scelto all'interno del metodo `getImageName()`.


3. **Compilazione:** Fase di sviluppo.
Compilare il progetto in modalità *Release* per generare il file binario `.dll` del plugin custom.


4. **Distribuzione (Deployment):** Fase di rilascio.
Inserire il file `.dll` generato all'interno della cartella dei `Plugins` dell'applicazione principale. Seguire le istruzioni specifiche per l'ambiente di runtime (vedere sotto).


### Opzione A: Installazione in ambiente Docker

Se l'applicazione viene eseguita tramite container Docker, è necessario mappare la cartella dei plugin dall'host al container.

1. Configurare un **bind mount** nel file `docker-compose.yml` o nel comando di run per la directory dei plugin.
2. Copiare la `.dll` del plugin nella cartella locale dell'host associata al volume.
3. Riavviare i container Docker con il comando:
```bash
docker compose restart

```

### Opzione B: Installazione Manuale (Senza Docker)
1. Copiare la `.dll` del plugin direttamente nella cartella `Plugins` situata nella directory di esecuzione del software.
2. Riavviare il processo dell'applicazione. Al boot, il sistema eseguirà la scansione della cartella tramite **Reflection** e caricherà il plugin in memoria.

---

## Configuration

### Gestione dei Conflitti Immagine
Nelle impostazioni di sistema è presente un flag di configurazione per gestire la priorità delle immagini in caso di sovrapposizione.

| Scenario | Comportamento con "Preferenza Locale" | Comportamento con "Preferenza API" |
| :--- | :--- | :--- |
| **L'utente inserisce manualmente un'immagine e l'API ne trova una secondaria** | Il sistema mantiene l'immagine caricata dall'utente. | L'immagine dell'utente viene sovrascritta (o nascosta) a favore di quella fetchata dall'API. |

---

## Notes & Known Limitations
*   **Meccanismo di Specularità:** Poiché il caricamento avviene tramite Reflection, assicurarsi che le classi dei plugin siano marcate come `public` e abbiano un costruttore pubblico senza parametri (o compatibile con l'Inversion of Control del core) per evitare fallimenti durante il boot.
*   **Prestazioni al boot:** Un numero elevato di DLL nella cartella Plugins potrebbe rallentare leggermente i tempi di avvio dell'applicazione a causa del tempo di scansione dell'assembly.

```