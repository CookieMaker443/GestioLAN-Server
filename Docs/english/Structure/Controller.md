# Items Module — GestioLan.API

## Overview

This module manages **Items** within the GestioLan API. It is responsible for creating, reading, updating, and deleting items stored in the database, including handling optional image associations and category filtering via bitmask.

The module is split into three distinct components following a **layered architecture**:

| Layer | File | Responsibility |
|---|---|---|
| Interface | `IItemService.cs` | Defines the contract that the service must implement |
| Service | `ItemService.cs` | Contains all business logic; is HTTP-agnostic |
| Controller | `ItemsController.cs` | Receives HTTP requests, delegates to the service, returns HTTP responses |

---

## Requirements / Dependencies

- [.NET 6+](https://dotnet.microsoft.com/) (ASP.NET Core)
- **Entity Framework Core** — ORM for database access (`Microsoft.EntityFrameworkCore`)
- `GestioLanContext` — the project's EF Core `DbContext`, exposing `Items` and `Images` and others DbSets
- `GestioLan.API.Models.Item` — the Item entity model

---

## Architecture

### Design Principles

The module follows the **Separation of Concerns** principle, splitting responsibilities across three layers:

```
HTTP Request
     │
     ▼
┌─────────────────────┐
│   ItemsController   │  ← Receives HTTP requests, maps exceptions to HTTP responses
└─────────────────────┘
          │  calls
          ▼
┌─────────────────────┐
│    IItemService     │  ← Interface (contract); enables mocking for unit tests
└─────────────────────┘
          │  implemented by
          ▼
┌─────────────────────┐
│     ItemService     │  ← Business logic; throws exceptions, never returns ActionResult
└─────────────────────┘
          │  queries
          ▼
┌─────────────────────┐
│   GestioLanContext  │  ← Entity Framework Core DbContext (database)
└─────────────────────┘
```

### Why the Interface Layer?

`IItemService` exists for two key reasons:

1. **Testability** — Unit tests can inject a mock implementation of `IItemService` to test controller behaviour without ever touching the real database.
2. **Decoupling** — The controller depends on an abstraction, not a concrete class, making future replacements or extensions easier.

### Service vs Controller — HTTP Agnosticism

The service layer is **completely unaware of HTTP**. It never uses `ActionResult`, `Ok()`, `NotFound()`, or any other ASP.NET HTTP construct.

Instead:
- On success, the service **returns a plain C# object** (e.g. `Item`, `IEnumerable<Item>`).
- On failure, the service **throws a standard C# exception** (e.g. `KeyNotFoundException`, `ArgumentException`).

The controller's job is to **catch those exceptions and translate them** into the appropriate HTTP response:

```csharp
// ✅ Correct — service method signature (HTTP-agnostic)
public async Task<Item> GetItemByIdAsync(int id)

// ❌ Wrong — service should never return ActionResult
public async Task<ActionResult<Item>> GetItemByIdAsync(int id)
```

### Async Naming Convention

All methods that use `async`/`await` follow the convention of appending `Async` to their name:

```csharp
GetItemsAsync(...)
GetItemByIdAsync(...)
CreateItemAsync(...)
DeleteItemAsync(...)
UpdateItemAsync(...)
```

---

## API Reference

### `IItemService` — Interface

Defines the contract for the Items service. Located at `src/GestioLan.API/Services/Items/IItemService.cs`.

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

Returns a filtered list of all items. All parameters are optional; omitting one skips that filter entirely.

| Parameter | Type | Description |
|---|---|---|
| `has_category` | `bool?` | If `true`, returns only items that have a category assigned. If `false`, only items without one. |
| `id_category` | `int?` | Filters by category using a **bitmask** — returns items whose category includes all bits set in this value. |
| `name` | `string?` | Filters by partial name match (case-sensitive, uses `Contains`). |
| `has_image` | `bool?` | If `true`, returns only items with an image. If `false`, only items without one. |
| `quantity` | `int?` | Returns only items with exactly this quantity. |
| `type_quantity` | `string?` | Returns only items matching this quantity type (e.g. `"pz"`, `"kg"`). |

**Returns:** `Task<IEnumerable<Item>>` — the filtered collection of items.

> ⚠️ **Note:** The `id_category` filter uses a bitmask comparison: `(item.IdCategory & id_category) == id_category`. This means a category value of `6` (binary `110`) would match items with category `7` (binary `111`) but not `5` (binary `101`).

---

#### `GetItemByIdAsync`

```csharp
Task<Item> GetItemByIdAsync(int id)
```

Retrieves a single item by its primary key.

| Parameter | Type | Description |
|---|---|---|
| `id` | `int` | The primary key of the item to retrieve. |

**Returns:** `Task<Item>` — the matching item.

**Throws:** `KeyNotFoundException` — if no item with the given `id` exists.

---

#### `CreateItemAsync`

```csharp
Task<Item> CreateItemAsync(Item item)
```

Persists a new item to the database. If the item references an image, the image's `ItemsCount` counter is incremented. If the referenced image does not exist, the item is saved without an image (no exception is thrown — a warning is logged to the console instead).

| Parameter | Type | Description |
|---|---|---|
| `item` | `Item` | The item object to create. |

**Returns:** `Task<Item>` — the created item, including its database-generated ID.

> ⚠️ **Note:** If `IdImage` is `0`, it is treated as `null` (no image). This normalises the value before any database operation.

---

#### `DeleteItemAsync`

```csharp
Task DeleteItemAsync(int id)
```

Deletes an item from the database. If the item had an associated image, its `ItemsCount` counter is decremented.

| Parameter | Type | Description |
|---|---|---|
| `id` | `int` | The primary key of the item to delete. |

**Returns:** `Task` (void async).

**Throws:** `KeyNotFoundException` — if no item with the given `id` exists.

---

#### `UpdateItemAsync`

```csharp
Task UpdateItemAsync(int id, Item updatedItem)
```

Updates an existing item. Handles all image counter logic:
- If the image changes to a **new valid image** → decrements old image counter, increments new one.
- If the image changes to a **non-existent image** → decrements old counter, clears image fields, logs a warning.
- If the image is set to **null** → decrements old counter, clears image fields.
- If the image is **unchanged** → no image counter operations are performed.

| Parameter | Type | Description |
|---|---|---|
| `id` | `int` | The ID of the item to update, taken from the URL. |
| `updatedItem` | `Item` | The updated item object from the request body. |

**Returns:** `Task` (void async).

**Throws:**
- `ArgumentException` — if `id` does not match `updatedItem.IdItem`.
- `KeyNotFoundException` — if no item with the given `id` exists.

---

### `ItemsController` — HTTP Endpoints

Receives HTTP requests and delegates work to `IItemService`. Translates service exceptions into HTTP responses. Located at `src/GestioLan.API/Controllers/ItemsController.cs`.

All endpoints require authentication (`[Authorize]`).

---

#### `GET /api/items/GetItems`

Retrieves a filtered list of items. All query parameters are optional.

**Query Parameters:** same as `GetItemsAsync` above.

| HTTP Response | Condition |
|---|---|
| `200 OK` | Returns the list (may be empty). |

---

#### `GET /api/items/GetItems/{id}`

Retrieves a single item by ID.

| HTTP Response | Condition |
|---|---|
| `200 OK` | Item found and returned. |
| `404 Not Found` | No item with the given `id`. |

---

#### `POST /api/items/CreateItem`

Creates a new item. The item object is provided in the request body as JSON.

| HTTP Response | Condition |
|---|---|
| `201 Created` | Item created successfully. Includes a `Location` header pointing to the new resource. |

---

#### `DELETE /api/items/DeleteItem/{id}`

Deletes an item by ID.

| HTTP Response | Condition |
|---|---|
| `204 No Content` | Item deleted successfully. |
| `404 Not Found` | No item with the given `id`. |

---

#### `PUT /api/items/ModifyItem/{id}`

Updates an existing item. The updated item object is provided in the request body as JSON.

| HTTP Response | Condition |
|---|---|
| `200 OK` | Item updated successfully. |
| `400 Bad Request` | The `id` in the URL does not match `IdItem` in the body. |
| `404 Not Found` | No item with the given `id`. |

---

## Configuration

Register the service in `Program.cs` using the **Scoped** lifetime (one instance per HTTP request):

```csharp
builder.Services.AddScoped<IItemService, ItemService>();
```

> ⚠️ **Note:** Using `AddScoped` is correct here because `ItemService` depends on `GestioLanContext`, which is itself typically scoped to the request.

---

## Examples

### Fetch all items in a specific category with an image

```http
GET /api/items/GetItems?id_category=4&has_image=true
Authorization: Bearer <token>
```

### Create a new item without an image

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

### Update an item and assign it a new image

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

## Notes & Known Limitations

- **Image counter consistency** — `ItemsCount` on the `Images` table is maintained manually by the service. If a database operation fails mid-way (e.g. `SaveChangesAsync` throws), the counter could become inconsistent. A database transaction (`BeginTransactionAsync`) would make these operations atomic.

- **Non-existent image silently ignored on create** — When `CreateItemAsync` is called with an `IdImage` that does not exist in the database, the item is saved without an image and only a console warning is printed. No exception is raised. Depending on requirements, this could be changed to throw an exception and return `400 Bad Request`.

- **Category bitmask filtering** — The `id_category` filter works as a bitmask inclusion check, not an exact equality check. This is intentional but should be clearly communicated to API consumers.

- **Testability** — Because the controller depends only on `IItemService`, you can inject a mock (e.g. via Moq) in unit tests to test all HTTP response scenarios without a real database.