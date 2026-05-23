# Modulo Items — GestioLan.API

## Panoramica

Questo modulo gestisce gli **Item** all'interno delle API GestioLan. Si occupa della creazione, lettura, aggiornamento ed eliminazione degli item nel database, inclusa la gestione opzionale delle immagini associate e il filtraggio per categoria tramite bitmask.

Il modulo è suddiviso in tre componenti distinti che seguono un'**architettura a strati**:

| Strato | File | Responsabilità |
|---|---|---|
| Interfaccia | `IItemService.cs` | Definisce il contratto che il servizio dovrà implementare |
| Servizio | `ItemService.cs` | Contiene tutta la logica di business; è agnostico rispetto a HTTP |
| Controller | `ItemsController.cs` | Riceve le richieste HTTP, delega al servizio, restituisce le risposte HTTP |

---

## Requisiti / Dipendenze

- [.NET 6+](https://dotnet.microsoft.com/) (ASP.NET Core)
- **Entity Framework Core** — ORM per l'accesso al database (`Microsoft.EntityFrameworkCore`)
- `GestioLanContext` — il `DbContext` EF Core del progetto, che espone i DbSet `Items` e `Images`
- `GestioLan.API.Models.Item` — il modello entità Item

---

## Architettura

### Principi di Design

Il modulo segue il principio della **Separazione delle Responsabilità** (*Separation of Concerns*), distribuendo le responsabilità su tre strati:

```
Richiesta HTTP
     │
     ▼
┌─────────────────────┐
│   ItemsController   │  ← Riceve le richieste HTTP, traduce le eccezioni in risposte HTTP
└─────────────────────┘
          │  chiama
          ▼
┌─────────────────────┐
│    IItemService     │  ← Interfaccia (contratto); consente il mocking nei test unitari
└─────────────────────┘
          │  implementata da
          ▼
┌─────────────────────┐
│     ItemService     │  ← Logica di business; lancia eccezioni, non restituisce mai ActionResult
└─────────────────────┘
          │  interroga
          ▼
┌─────────────────────┐
│   GestioLanContext  │  ← DbContext di Entity Framework Core (database)
└─────────────────────┘
```

### Perché il Livello Interfaccia?

`IItemService` esiste per due motivi principali:

1. **Testabilità** — I test unitari possono iniettare un'implementazione mock di `IItemService` per testare il comportamento del controller senza mai accedere al database reale.
2. **Disaccoppiamento** — Il controller dipende da un'astrazione, non da una classe concreta, rendendo più semplici eventuali sostituzioni o estensioni future.

### Servizio vs Controller — Agnosticismo HTTP

Il livello servizio è **completamente ignaro di HTTP**. Non usa mai `ActionResult`, `Ok()`, `NotFound()` né qualsiasi altro costrutto HTTP di ASP.NET.

Al contrario:
- In caso di **successo**, il servizio **restituisce un oggetto C# semplice** (es. `Item`, `IEnumerable<Item>`).
- In caso di **errore**, il servizio **lancia una eccezione C# standard** (es. `KeyNotFoundException`, `ArgumentException`).

Il compito del controller è **intercettare quelle eccezioni e tradurle** nella risposta HTTP appropriata:

```csharp
// ✅ Corretto — firma del metodo del servizio (agnostico rispetto a HTTP)
public async Task<Item> GetItemByIdAsync(int id)

// ❌ Sbagliato — il servizio non deve mai restituire ActionResult
public async Task<ActionResult<Item>> GetItemByIdAsync(int id)
```

### Convenzione di Nomenclatura Async

Tutti i metodi che usano `async`/`await` seguono la convenzione di aggiungere il suffisso `Async` al nome:

```csharp
GetItemsAsync(...)
GetItemByIdAsync(...)
CreateItemAsync(...)
DeleteItemAsync(...)
UpdateItemAsync(...)
```

---

## Riferimento API

### `IItemService` — Interfaccia

Definisce il contratto per il servizio Items. Si trova in `src/GestioLan.API/Services/Items/IItemService.cs`.

---

#### `GetItemsAsync`

```csharp
Task<IEnumerable<Item>> GetItemsAsync(
    bool? has_category,
    int? id_category,
    string? name,
    bool? has_image,
    int? quantity,
    string? type_quantity
)
```

Restituisce una lista filtrata di tutti gli item. Tutti i parametri sono opzionali; omettendone uno, il relativo filtro viene semplicemente ignorato.

| Parametro | Tipo | Descrizione |
|---|---|---|
| `has_category` | `bool?` | Se `true`, restituisce solo gli item con una categoria assegnata. Se `false`, solo quelli senza. |
| `id_category` | `int?` | Filtra per categoria tramite **bitmask** — restituisce gli item la cui categoria include tutti i bit impostati in questo valore. |
| `name` | `string?` | Filtra per corrispondenza parziale del nome (case-sensitive, usa `Contains`). |
| `has_image` | `bool?` | Se `true`, restituisce solo gli item con un'immagine. Se `false`, solo quelli senza. |
| `quantity` | `int?` | Restituisce solo gli item con esattamente questa quantità. |
| `type_quantity` | `string?` | Restituisce solo gli item con questo tipo di quantità (es. `"pz"`, `"kg"`). |

**Restituisce:** `Task<IEnumerable<Item>>` — la collezione filtrata di item.

> ⚠️ **Nota:** Il filtro `id_category` usa un confronto bitmask: `(item.IdCategory & id_category) == id_category`. Questo significa che un valore di categoria `6` (binario `110`) corrisponderà agli item con categoria `7` (binario `111`), ma non a quelli con categoria `5` (binario `101`).

---

#### `GetItemByIdAsync`

```csharp
Task<Item> GetItemByIdAsync(int id)
```

Recupera un singolo item tramite la sua chiave primaria.

| Parametro | Tipo | Descrizione |
|---|---|---|
| `id` | `int` | La chiave primaria dell'item da recuperare. |

**Restituisce:** `Task<Item>` — l'item corrispondente.

**Lancia:** `KeyNotFoundException` — se nessun item con l'`id` specificato esiste nel database.

---

#### `CreateItemAsync`

```csharp
Task<Item> CreateItemAsync(Item item)
```

Salva un nuovo item nel database. Se l'item fa riferimento a un'immagine, il contatore `ItemsCount` di quell'immagine viene incrementato. Se l'immagine referenziata non esiste, l'item viene salvato senza immagine (non viene lanciata alcuna eccezione — viene invece stampato un avviso in console).

| Parametro | Tipo | Descrizione |
|---|---|---|
| `item` | `Item` | L'oggetto item da creare. |

**Restituisce:** `Task<Item>` — l'item creato, incluso l'ID generato dal database.

> ⚠️ **Nota:** Se `IdImage` vale `0`, viene trattato come `null` (nessuna immagine). Questo normalizza il valore prima di qualsiasi operazione sul database.

---

#### `DeleteItemAsync`

```csharp
Task DeleteItemAsync(int id)
```

Elimina un item dal database. Se l'item aveva un'immagine associata, il suo contatore `ItemsCount` viene decrementato.

| Parametro | Tipo | Descrizione |
|---|---|---|
| `id` | `int` | La chiave primaria dell'item da eliminare. |

**Restituisce:** `Task` (void async).

**Lancia:** `KeyNotFoundException` — se nessun item con l'`id` specificato esiste nel database.

---

#### `UpdateItemAsync`

```csharp
Task UpdateItemAsync(int id, Item updatedItem)
```

Aggiorna un item esistente. Gestisce tutta la logica dei contatori delle immagini:
- Se l'immagine cambia con una **nuova immagine valida** → decrementa il contatore della vecchia, incrementa quello della nuova.
- Se l'immagine cambia con una **immagine non esistente** → decrementa il vecchio contatore, azzera i campi immagine, stampa un avviso.
- Se l'immagine viene impostata a **null** → decrementa il vecchio contatore, azzera i campi immagine.
- Se l'immagine è **invariata** → nessuna operazione sui contatori viene eseguita.

| Parametro | Tipo | Descrizione |
|---|---|---|
| `id` | `int` | L'ID dell'item da aggiornare, ricavato dall'URL. |
| `updatedItem` | `Item` | L'oggetto item aggiornato proveniente dal corpo della richiesta. |

**Restituisce:** `Task` (void async).

**Lancia:**
- `ArgumentException` — se `id` non corrisponde a `updatedItem.IdItem`.
- `KeyNotFoundException` — se nessun item con l'`id` specificato esiste nel database.

---

### `ItemsController` — Endpoint HTTP

Riceve le richieste HTTP e delega il lavoro a `IItemService`. Traduce le eccezioni del servizio in risposte HTTP. Si trova in `src/GestioLan.API/Controllers/ItemsController.cs`.

Tutti gli endpoint richiedono autenticazione (`[Authorize]`).

---

#### `GET /api/items/GetItems`

Recupera una lista filtrata di item. Tutti i parametri di query sono opzionali.

**Parametri di query:** stessi di `GetItemsAsync` sopra.

| Risposta HTTP | Condizione |
|---|---|
| `200 OK` | Restituisce la lista (può essere vuota). |

---

#### `GET /api/items/GetItems/{id}`

Recupera un singolo item tramite ID.

| Risposta HTTP | Condizione |
|---|---|
| `200 OK` | Item trovato e restituito. |
| `404 Not Found` | Nessun item con l'`id` specificato. |

---

#### `POST /api/items/CreateItem`

Crea un nuovo item. L'oggetto item viene fornito nel corpo della richiesta come JSON.

| Risposta HTTP | Condizione |
|---|---|
| `201 Created` | Item creato con successo. Include un header `Location` che punta alla nuova risorsa. |

---

#### `DELETE /api/items/DeleteItem/{id}`

Elimina un item tramite ID.

| Risposta HTTP | Condizione |
|---|---|
| `204 No Content` | Item eliminato con successo. |
| `404 Not Found` | Nessun item con l'`id` specificato. |

---

#### `PUT /api/items/ModifyItem/{id}`

Aggiorna un item esistente. L'oggetto item aggiornato viene fornito nel corpo della richiesta come JSON.

| Risposta HTTP | Condizione |
|---|---|
| `200 OK` | Item aggiornato con successo. |
| `400 Bad Request` | L'`id` nell'URL non corrisponde a `IdItem` nel corpo della richiesta. |
| `404 Not Found` | Nessun item con l'`id` specificato. |

---

## Configurazione

Registrare il servizio in `Program.cs` usando il ciclo di vita **Scoped** (una istanza per ogni richiesta HTTP):

```csharp
builder.Services.AddScoped<IItemService, ItemService>();
```

> ⚠️ **Nota:** Usare `AddScoped` è corretto in questo caso perché `ItemService` dipende da `GestioLanContext`, il quale è anch'esso tipicamente registrato con ciclo di vita scoped.

---

## Esempi

### Recuperare tutti gli item di una categoria specifica che hanno un'immagine

```http
GET /api/items/GetItems?id_category=4&has_image=true
Authorization: Bearer <token>
```

### Creare un nuovo item senza immagine

```json
POST /api/items/CreateItem
Content-Type: application/json

{
  "itemName": "Cavo HDMI",
  "description": "Cavo HDMI 2m",
  "idCategory": 2,
  "quantity": 5,
  "typeQuantity": "pz",
  "idImage": null
}
```

### Aggiornare un item e assegnargli una nuova immagine

```json
PUT /api/items/ModifyItem/12
Content-Type: application/json

{
  "idItem": 12,
  "itemName": "Cavo HDMI",
  "description": "Cavo HDMI 2m — aggiornato",
  "idCategory": 2,
  "quantity": 3,
  "typeQuantity": "pz",
  "idImage": 7
}
```

---

## Note e Limitazioni Conosciute

- **Consistenza del contatore immagini** — `ItemsCount` nella tabella `Images` viene gestito manualmente dal servizio. Se un'operazione sul database fallisce a metà (es. `SaveChangesAsync` lancia un'eccezione), il contatore potrebbe diventare inconsistente. Racchiudere le operazioni in una transazione (`BeginTransactionAsync`) le renderebbe atomiche.

- **Immagine inesistente ignorata silenziosamente in fase di creazione** — Quando `CreateItemAsync` viene chiamato con un `IdImage` che non esiste nel database, l'item viene salvato senza immagine e viene stampato solo un avviso in console. Non viene lanciata alcuna eccezione. A seconda dei requisiti, questo comportamento potrebbe essere modificato per lanciare un'eccezione e restituire `400 Bad Request`.

- **Filtro categoria con bitmask** — Il filtro `id_category` funziona come un controllo di inclusione bitmask, non come un confronto di uguaglianza esatta. Questo è intenzionale ma dovrebbe essere comunicato chiaramente ai consumatori delle API.

- **Testabilità** — Poiché il controller dipende solo da `IItemService`, è possibile iniettare un mock (es. tramite Moq) nei test unitari per testare tutti gli scenari di risposta HTTP senza un database reale.