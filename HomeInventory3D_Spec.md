# HomeInventory3D — Project Specification & Claude Code Prompt

## Overview

**HomeInventory3D** — система умного домашнего инвентаря с Digital Twin. Пользователь сканирует содержимое контейнеров (коробок) с помощью iPad LiDAR, загружает 3D-скан через Angular web-приложение, бэкенд обрабатывает и сегментирует объекты, а Unity-клиент отображает виртуальную копию контейнера в реальном времени. Голосовой ассистент (Home Assistant / Алиса) позволяет искать предметы голосом.

---

## Tech Stack (строго зафиксирован)

| Layer | Technology | Version |
|-------|-----------|---------|
| Backend | ASP.NET Core Web API | .NET 10+ (latest preview/stable) |
| Language (Backend) | C# | Latest (C# 13+) |
| Database | PostgreSQL | 18 |
| ORM | Entity Framework Core | Matching .NET version |
| Frontend | Angular | Latest stable (19+) |
| 3D Client | Unity | 6.4 (6000.4.1f1) |
| Unity Template | Universal 3D (URP) | |
| Language (Unity) | C# | 14 |
| Realtime | SignalR | Built-in ASP.NET Core |
| File Storage | Local filesystem (MVP) → MinIO (prod) | |
| Search | PostgreSQL pg_trgm + full-text search | |
| Containerization | Docker Compose | |

---

## Architecture

```
┌─────────────────┐     ┌──────────────────┐     ┌─────────────────┐
│   iPad Pro      │     │  Angular Web App  │     │  Unity Client   │
│   (LiDAR Scan)  │     │  (Upload + Mgmt)  │     │  (Digital Twin)  │
│                 │     │                   │     │                  │
│  Export .obj    │────>│  Upload 3D file   │     │  SignalR client  │
│  .ply / .usdz   │     │  Manage containers│     │  3D scene render │
│                 │     │  Search items     │     │  Spawn animations│
└─────────────────┘     └────────┬──────────┘     └────────┬─────────┘
                                 │                          │
                                 │ HTTP REST                │ SignalR WebSocket
                                 │                          │
                        ┌────────▼──────────────────────────▼─────────┐
                        │         ASP.NET Core Backend                │
                        │                                             │
                        │  ┌─────────────┐  ┌──────────────────────┐  │
                        │  │ REST API     │  │ SignalR Hub          │  │
                        │  │ Controllers  │  │ (InventoryHub)       │  │
                        │  └──────┬──────┘  └──────────┬───────────┘  │
                        │         │                     │              │
                        │  ┌──────▼─────────────────────▼───────────┐ │
                        │  │  Application Services                  │ │
                        │  │  ContainerService, ItemService,        │ │
                        │  │  ScanService, VisionService            │ │
                        │  └──────┬─────────────────────────────────┘ │
                        │         │                                    │
                        │  ┌──────▼──────────────┐  ┌──────────────┐  │
                        │  │ Background Worker   │  │ File Storage │  │
                        │  │ (3D Processing      │  │ (Local/MinIO)│  │
                        │  │  Pipeline)          │  │              │  │
                        │  └──────┬──────────────┘  └──────────────┘  │
                        │         │                                    │
                        │  ┌──────▼──────────────┐                    │
                        │  │ PostgreSQL 18       │                    │
                        │  │ + pg_trgm           │                    │
                        │  └─────────────────────┘                    │
                        └─────────────────────────────────────────────┘
```

---

## Solution Structure (Backend)

```
HomeInventory3D/
├── HomeInventory3D.sln
├── docker-compose.yml
├── src/
│   ├── HomeInventory3D.Api/
│   │   ├── Controllers/
│   │   │   ├── ContainersController.cs
│   │   │   ├── ItemsController.cs
│   │   │   ├── ScansController.cs
│   │   │   └── VoiceController.cs
│   │   ├── Hubs/
│   │   │   └── InventoryHub.cs
│   │   ├── Configuration/
│   │   │   └── DependencyInjection.cs
│   │   ├── Program.cs
│   │   └── appsettings.json
│   │
│   ├── HomeInventory3D.Application/
│   │   ├── Interfaces/
│   │   │   ├── IContainerRepository.cs
│   │   │   ├── IItemRepository.cs
│   │   │   ├── IScanSessionRepository.cs
│   │   │   ├── IFileStorageService.cs
│   │   │   └── IVisionRecognitionService.cs
│   │   ├── Services/
│   │   │   ├── ContainerService.cs
│   │   │   ├── ItemService.cs
│   │   │   └── ScanService.cs
│   │   ├── DTOs/
│   │   │   ├── ContainerDtos.cs
│   │   │   ├── ItemDtos.cs
│   │   │   ├── ScanDtos.cs
│   │   │   ├── SceneDtos.cs
│   │   │   └── VoiceDtos.cs
│   │   └── BackgroundJobs/
│   │       └── ScanProcessingWorker.cs
│   │
│   ├── HomeInventory3D.Domain/
│   │   ├── Entities/
│   │   │   ├── Container.cs
│   │   │   ├── InventoryItem.cs
│   │   │   └── ScanSession.cs
│   │   └── Enums/
│   │       ├── ItemStatus.cs
│   │       └── ScanType.cs
│   │
│   └── HomeInventory3D.Infrastructure/
│       ├── Persistence/
│       │   ├── InventoryDbContext.cs
│       │   ├── Configurations/
│       │   │   ├── ContainerConfiguration.cs
│       │   │   ├── InventoryItemConfiguration.cs
│       │   │   └── ScanSessionConfiguration.cs
│       │   └── Repositories/
│       │       ├── ContainerRepository.cs
│       │       ├── ItemRepository.cs
│       │       └── ScanSessionRepository.cs
│       ├── Storage/
│       │   └── LocalFileStorageService.cs
│       └── Vision/
│           └── ClaudeVisionService.cs
│
├── client/                          # Angular
│   └── home-inventory-web/
│
└── tests/
    └── HomeInventory3D.Tests/
```

---

## Database Schema (PostgreSQL 18)

### Требования
- Использовать расширение `pg_trgm` для fuzzy search
- Все таблицы с UUID primary keys
- Timestamps в UTC
- Tags как `text[]` (native PostgreSQL array)
- Индексы GIN для полнотекстового поиска

### Таблицы

```sql
CREATE EXTENSION IF NOT EXISTS pg_trgm;

CREATE TABLE containers (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(255) NOT NULL,
    nfc_id VARCHAR(100) UNIQUE,
    qr_code VARCHAR(500) UNIQUE,
    location VARCHAR(500) NOT NULL,
    description TEXT,
    
    -- Physical dimensions (millimeters)
    width_mm REAL,
    height_mm REAL,
    depth_mm REAL,
    
    -- 3D assets
    mesh_file_path VARCHAR(1000),
    thumbnail_path VARCHAR(1000),
    
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    last_scanned_at TIMESTAMPTZ
);

CREATE TABLE inventory_items (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    container_id UUID NOT NULL REFERENCES containers(id) ON DELETE CASCADE,
    name VARCHAR(500) NOT NULL,
    tags TEXT[] DEFAULT '{}',
    description TEXT,
    
    -- Position inside container (relative 0.0 - 1.0)
    position_x REAL,
    position_y REAL,
    position_z REAL,
    
    -- Bounding box (relative)
    bbox_min_x REAL, bbox_min_y REAL, bbox_min_z REAL,
    bbox_max_x REAL, bbox_max_y REAL, bbox_max_z REAL,
    
    -- Rotation (euler degrees)
    rotation_x REAL,
    rotation_y REAL,
    rotation_z REAL,
    
    -- Assets
    photo_path VARCHAR(1000),
    mesh_file_path VARCHAR(1000),
    thumbnail_path VARCHAR(1000),
    
    -- AI recognition
    confidence REAL,
    recognition_source VARCHAR(50), -- 'claude-vision', 'yolo', 'manual'
    
    status VARCHAR(20) NOT NULL DEFAULT 'Present', -- Present, Removed, Moved
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_items_name_trgm ON inventory_items USING gin (name gin_trgm_ops);
CREATE INDEX idx_items_tags ON inventory_items USING gin (tags);
CREATE INDEX idx_items_container ON inventory_items (container_id);
CREATE INDEX idx_items_status ON inventory_items (status);

CREATE TABLE scan_sessions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    container_id UUID NOT NULL REFERENCES containers(id) ON DELETE CASCADE,
    scan_type VARCHAR(20) NOT NULL, -- 'manual', 'lidar', 'automatic'
    
    point_cloud_path VARCHAR(1000),
    depth_map_path VARCHAR(1000),
    rgb_photo_path VARCHAR(1000),
    
    items_detected INT NOT NULL DEFAULT 0,
    items_added INT NOT NULL DEFAULT 0,
    items_removed INT NOT NULL DEFAULT 0,
    
    status VARCHAR(20) NOT NULL DEFAULT 'Pending', -- Pending, Processing, Completed, Failed
    error_message TEXT,
    
    scanned_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

---

## API Endpoints

### Containers
| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/containers` | List all containers |
| GET | `/api/containers/{id}` | Get container by ID |
| POST | `/api/containers` | Create container |
| PUT | `/api/containers/{id}` | Update container |
| DELETE | `/api/containers/{id}` | Delete container |
| GET | `/api/containers/{id}/scene` | **Get 3D scene data for Unity** (container mesh + all items with positions) |
| GET | `/api/containers/nfc/{nfcId}` | Find container by NFC tag |

### Items
| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/items?containerId={id}` | List items in container |
| GET | `/api/items/{id}` | Get item by ID |
| POST | `/api/items` | Create item manually |
| PUT | `/api/items/{id}` | Update item |
| DELETE | `/api/items/{id}` | Delete item |
| GET | `/api/items/search?q={query}&limit=20` | **Full-text search (pg_trgm)** |
| PATCH | `/api/items/{id}/status` | Change status (Present/Removed/Moved) |

### Scans
| Method | Route | Description |
|--------|-------|-------------|
| POST | `/api/scans/upload` | **Upload 3D scan file** (multipart: file + containerId + scanType) |
| GET | `/api/scans?containerId={id}` | List scan history for container |
| GET | `/api/scans/{id}` | Get scan details |

### Voice (Home Assistant integration)
| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/voice/search?q={query}` | **Voice search** — returns plain text answer + structured results |

### Files
| Method | Route | Description |
|--------|-------|-------------|
| GET | `/files/{**path}` | Serve uploaded files (photos, meshes, point clouds) |

---

## SignalR Hub: `InventoryHub`

**Endpoint:** `/hubs/inventory`

### Server → Client Events

```csharp
public interface IInventoryClient
{
    // Scan processing progress
    Task ScanProgress(Guid scanId, Guid containerId, int progressPercent, string stage);
    
    // Scan completed
    Task ScanCompleted(Guid scanId, Guid containerId, int itemsDetected, int itemsAdded, int itemsRemoved);
    
    // Individual item added (Unity uses this to spawn objects one by one)
    Task ItemAdded(ItemAddedEvent item);
    
    // Item removed
    Task ItemRemoved(Guid itemId, Guid containerId);
    
    // Scan failed
    Task ScanFailed(Guid scanId, string errorMessage);
}

public record ItemAddedEvent(
    Guid Id,
    Guid ContainerId,
    string Name,
    string[] Tags,
    float? PositionX, float? PositionY, float? PositionZ,
    float? RotationX, float? RotationY, float? RotationZ,
    float? BboxMinX, float? BboxMinY, float? BboxMinZ,
    float? BboxMaxX, float? BboxMaxY, float? BboxMaxZ,
    string? MeshUrl,
    string? ThumbnailUrl,
    float? Confidence);
```

### Client → Server Methods

```csharp
// Join container's real-time updates
Task JoinContainer(Guid containerId);

// Leave container updates  
Task LeaveContainer(Guid containerId);
```

**Логика групп:** Каждый контейнер = SignalR group. Unity-клиент вызывает `JoinContainer` при открытии сундука. События отправляются только в группу контейнера.

---

## Background Processing Pipeline

### Flow

```
Upload .obj/.ply/.glb
    │
    ▼
1. Validate file format + size
    │
    ▼
2. Save raw file to storage
    │  → SignalR: ScanProgress(10%, "Uploading")
    ▼
3. Parse 3D file
    │  Library: Assimp.NET (AssimpNet NuGet)
    │  Extract: vertices, faces, materials, textures
    │  → SignalR: ScanProgress(30%, "Parsing 3D file")
    ▼
4. Segment objects
    │  Approach 1 (MVP): Separate meshes by material/group
    │  Approach 2 (future): SAM-based segmentation from rendered views
    │  → SignalR: ScanProgress(50%, "Segmenting objects")
    ▼
5. For each segmented object:
    │
    ├── Calculate bounding box + center position (relative to container)
    ├── Export individual mesh as .glb
    ├── Render thumbnail (top-down view)
    ├── Send rendered view to Claude Vision API for labeling
    │   Prompt: "Identify this object. Return JSON: { name, tags[], description }"
    │
    │  → SignalR: ItemAdded(item) — **Unity spawns object here**
    │  → SignalR: ScanProgress(50% + N%, "Recognized: отвёртка")
    ▼
6. Save all items to PostgreSQL
    │
    ▼
7. Update ScanSession status = Completed
    → SignalR: ScanCompleted(scanId, itemCount)
```

### Implementation Notes

- Используй `IHostedService` + `Channel<T>` для очереди задач (MVP)
- Или Hangfire для production (retry, dashboard, scheduling)
- `AssimpNet` NuGet для парсинга 3D файлов (поддерживает .obj, .ply, .fbx, .glb, .3ds)
- Claude Vision API: POST to `https://api.anthropic.com/v1/messages` с image content
- Каждый объект после распознавания **немедленно** отправляется через SignalR, не дожидаясь всех остальных

---

## Angular Web App

### Pages

1. **Dashboard** (`/`)
   - Grid карточек контейнеров с thumbnail, item count, location
   - Кнопка "Add Container"
   - Search bar (глобальный поиск предметов)

2. **Container Detail** (`/containers/:id`)
   - Информация о контейнере (name, location, dimensions)
   - Список предметов с фото и тегами
   - Кнопка "Upload Scan"
   - Scan history timeline

3. **Upload Scan** (`/containers/:id/scan`)
   - Drag & Drop зона для .obj / .ply / .glb файлов
   - Выбор типа скана (Manual / LiDAR)
   - **Real-time progress** через SignalR
   - Предметы появляются в списке по мере распознавания

4. **Search** (`/search?q=...`)
   - Full-text поиск по названиям и тегам
   - Результаты с указанием контейнера и локации

5. **Settings** (`/settings`)
   - API keys (Claude Vision)
   - Home Assistant integration URL
   - File storage path

### SignalR Integration (Angular)

```typescript
// Use @microsoft/signalr package
import * as signalR from '@microsoft/signalr';

const connection = new signalR.HubConnectionBuilder()
    .withUrl('/hubs/inventory')
    .withAutomaticReconnect()
    .build();

connection.on('ScanProgress', (scanId, containerId, progress, stage) => {
    // Update progress bar
});

connection.on('ItemAdded', (item) => {
    // Add item to list with animation
});

connection.on('ScanCompleted', (scanId, containerId, detected, added, removed) => {
    // Show completion notification
});
```

---

## Unity Client

### Template: Universal 3D (URP)

### Project Structure

```
Assets/
├── Scripts/
│   ├── Networking/
│   │   ├── ApiClient.cs           // HTTP REST client (UnityWebRequest)
│   │   ├── SignalRClient.cs       // SignalR connection + event handlers
│   │   └── DTOs.cs                // Matching backend DTOs
│   │
│   ├── Scene/
│   │   ├── ContainerManager.cs    // Manages virtual chest/container
│   │   ├── ItemSpawner.cs         // Spawns 3D objects from mesh URLs
│   │   ├── ItemController.cs      // Per-object behavior (hover, select)
│   │   └── SceneLoader.cs         // Loads full scene from /api/containers/{id}/scene
│   │
│   ├── UI/
│   │   ├── SearchPanel.cs         // Search bar + results
│   │   ├── ItemInfoCard.cs        // Item details popup
│   │   ├── ToastNotification.cs   // "Отвёртка добавлена!" notifications
│   │   └── ProgressOverlay.cs     // Scan progress display
│   │
│   └── Animation/
│       ├── SpawnAnimation.cs      // Scale up + glow effect for new items
│       └── HighlightAnimation.cs  // Pulsing highlight for search results
│
├── Prefabs/
│   ├── GenericItem.prefab         // Default cube/shape for unrecognized items
│   ├── Container.prefab           // Virtual chest model
│   └── UI/
│       ├── ItemCard.prefab
│       └── Toast.prefab
│
├── Materials/
│   ├── SpawnGlow.mat              // Emission material for spawn effect
│   ├── Highlight.mat              // Pulsing highlight material
│   └── ContainerWood.mat          // Default container material
│
└── Scenes/
    └── MainScene.unity
```

### SignalR in Unity

Используй пакет `com.community.netcode.transport.signalr` или `Microsoft.AspNetCore.SignalR.Client` NuGet через NuGetForUnity.

```csharp
// SignalRClient.cs
using Microsoft.AspNetCore.SignalR.Client;

public class SignalRClient : MonoBehaviour
{
    private HubConnection _connection;
    
    public event Action<ItemAddedEvent> OnItemAdded;
    public event Action<Guid, Guid, int, string> OnScanProgress;
    public event Action<Guid, Guid, int, int, int> OnScanCompleted;

    async void Start()
    {
        _connection = new HubConnectionBuilder()
            .WithUrl("http://YOUR_BACKEND/hubs/inventory")
            .WithAutomaticReconnect()
            .Build();

        _connection.On<ItemAddedEvent>("ItemAdded", item =>
        {
            // Must dispatch to main thread
            UnityMainThread.Enqueue(() => OnItemAdded?.Invoke(item));
        });

        _connection.On<Guid, Guid, int, string>("ScanProgress", (scanId, containerId, progress, stage) =>
        {
            UnityMainThread.Enqueue(() => OnScanProgress?.Invoke(scanId, containerId, progress, stage));
        });

        await _connection.StartAsync();
    }

    public async Task JoinContainer(Guid containerId)
    {
        await _connection.InvokeAsync("JoinContainer", containerId);
    }
}
```

### Spawn Animation Flow

```csharp
// ItemSpawner.cs
public class ItemSpawner : MonoBehaviour
{
    [SerializeField] private GameObject genericItemPrefab;
    [SerializeField] private Material spawnGlowMaterial;
    
    public async void SpawnItem(ItemAddedEvent item)
    {
        // 1. Create placeholder at position
        var pos = new Vector3(
            item.PositionX ?? 0, 
            item.PositionY ?? 0, 
            item.PositionZ ?? 0);
        
        var obj = Instantiate(genericItemPrefab, pos, Quaternion.identity);
        obj.transform.localScale = Vector3.zero;
        
        // 2. Download mesh if available
        if (!string.IsNullOrEmpty(item.MeshUrl))
        {
            var mesh = await MeshDownloader.LoadGlb(item.MeshUrl);
            if (mesh != null)
            {
                // Replace generic with downloaded mesh
                ReplaceWithMesh(obj, mesh);
            }
        }
        
        // 3. Spawn animation: scale 0 → 1 with glow
        StartCoroutine(AnimateSpawn(obj));
    }

    IEnumerator AnimateSpawn(GameObject obj)
    {
        var renderer = obj.GetComponentInChildren<Renderer>();
        var originalMaterial = renderer.material;
        renderer.material = spawnGlowMaterial;
        
        float duration = 0.6f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float scale = Mathf.SmoothStep(0, 1, t);
            obj.transform.localScale = Vector3.one * scale;
            
            // Fade glow
            var color = spawnGlowMaterial.GetColor("_EmissionColor");
            spawnGlowMaterial.SetColor("_EmissionColor", color * (1 - t));
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        obj.transform.localScale = Vector3.one;
        renderer.material = originalMaterial;
    }
}
```

### GLB Mesh Loading

Используй пакет **GLTFUtility** или **UnityGLTF** для загрузки .glb файлов в runtime:

```csharp
// Через UnityGLTF (рекомендуется для Unity 6)
using UnityGLTF;

public static class MeshDownloader
{
    public static async Task<GameObject> LoadGlb(string url)
    {
        using var request = UnityWebRequest.Get(url);
        await request.SendWebRequest();
        
        if (request.result != UnityWebRequest.Result.Success)
            return null;
        
        var bytes = request.downloadHandler.data;
        // Use GLTFUtility to instantiate from bytes
        var obj = Importer.LoadFromBytes(bytes);
        return obj;
    }
}
```

---

## Home Assistant Integration

### Custom REST Command

```yaml
# configuration.yaml
rest_command:
  inventory_search:
    url: "http://YOUR_BACKEND_IP:5000/api/voice/search?q={{ query }}"
    method: GET

# automations.yaml  
automation:
  - alias: "Voice Inventory Search"
    trigger:
      platform: conversation
      command:
        - "где {item}"
        - "найди {item}"
        - "where is {item}"
    action:
      - service: rest_command.inventory_search
        data:
          query: "{{ trigger.slots.item }}"
        response_variable: result
      - service: tts.speak
        data:
          message: "{{ result.content.answer }}"
```

### Яндекс Алиса (Навык)

Создать навык в Яндекс.Диалогах → Webhook → твой backend endpoint:

```
POST /api/voice/alice
```

Backend парсит `request.nlu.tokens`, вызывает ItemService.SearchAsync, возвращает ответ в формате Алисы.

---

## Docker Compose (полный)

```yaml
services:
  postgres:
    image: postgres:18-alpine
    environment:
      POSTGRES_DB: home_inventory_3d
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
    ports:
      - "5432:5432"
    volumes:
      - pgdata:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres"]
      interval: 5s
      timeout: 5s
      retries: 5

  backend:
    build:
      context: .
      dockerfile: src/HomeInventory3D.Api/Dockerfile
    ports:
      - "5000:8080"
    environment:
      - ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=home_inventory_3d;Username=postgres;Password=postgres
      - Storage__BasePath=/app/storage
      - Storage__BaseUrl=http://localhost:5000
      - Claude__ApiKey=${CLAUDE_API_KEY}
    volumes:
      - storage:/app/storage
    depends_on:
      postgres:
        condition: service_healthy

  angular:
    build:
      context: ./client/home-inventory-web
      dockerfile: Dockerfile
    ports:
      - "4200:80"
    depends_on:
      - backend

volumes:
  pgdata:
  storage:
```

---

## Claude Vision Integration

### Service Implementation

```csharp
public class ClaudeVisionService : IVisionRecognitionService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public async Task<List<RecognizedItem>> RecognizeItemsAsync(
        Stream photo, string? containerContext, CancellationToken ct)
    {
        var bytes = await ReadStreamAsync(photo, ct);
        var base64 = Convert.ToBase64String(bytes);

        var request = new
        {
            model = "claude-sonnet-4-20250514",
            max_tokens = 4096,
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new
                        {
                            type = "image",
                            source = new
                            {
                                type = "base64",
                                media_type = "image/jpeg",
                                data = base64
                            }
                        },
                        new
                        {
                            type = "text",
                            text = @"Analyze this image of items in a storage container. 
                            For each distinct object visible, return a JSON array:
                            [
                              {
                                ""name"": ""item name in Russian"",
                                ""tags"": [""tag1"", ""tag2""],
                                ""description"": ""brief description"",
                                ""confidence"": 0.95,
                                ""position_x"": 0.3,  // relative 0-1 from left
                                ""position_y"": 0.7,  // relative 0-1 from top
                                ""bbox"": [x_min, y_min, x_max, y_max]  // relative 0-1
                              }
                            ]
                            Return ONLY valid JSON array, no markdown."
                        }
                    }
                }
            }
        };

        var response = await _httpClient.PostAsJsonAsync(
            "https://api.anthropic.com/v1/messages", request, ct);
        
        // Parse response and map to RecognizedItem list
    }
}
```

---

## Порядок реализации (рекомендуемый)

### Phase 1: Backend Core
1. Создать solution с Clean Architecture
2. Domain entities
3. EF Core DbContext + configurations + migrations
4. Repositories
5. Application services (ContainerService, ItemService)
6. REST API controllers
7. Docker Compose (PostgreSQL)
8. Swagger

### Phase 2: SignalR + Background Processing
1. SignalR Hub (InventoryHub)
2. Background worker с Channel<T>
3. File upload endpoint (multipart)
4. 3D file parsing (AssimpNet)
5. Claude Vision integration
6. Real-time events (ScanProgress, ItemAdded, ScanCompleted)

### Phase 3: Angular Frontend
1. Angular project setup
2. Container CRUD pages
3. Upload scan page with drag & drop
4. SignalR integration (real-time progress)
5. Search page
6. Responsive design (tablet-friendly)

### Phase 4: Unity Client
1. Unity 6.4 project (Universal 3D)
2. SignalR client connection
3. REST client (scene loading)
4. Container/chest 3D model
5. Item spawning from mesh URLs (GLB)
6. Spawn animation (scale + glow)
7. Search + highlight
8. Camera controls (orbit, zoom)

### Phase 5: Voice Integration
1. Home Assistant REST command
2. Yandex Alice skill webhook
3. Voice search endpoint

---

## Key NuGet Packages (Backend)

```xml
<!-- Core -->
<PackageReference Include="Microsoft.EntityFrameworkCore" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" />

<!-- 3D Processing -->
<PackageReference Include="AssimpNet" Version="5.0.0-beta1" />

<!-- SignalR is built-in ASP.NET Core -->

<!-- Optional -->
<PackageReference Include="Hangfire" />                    <!-- Job queue (if not using Channel<T>) -->
<PackageReference Include="Swashbuckle.AspNetCore" />      <!-- Swagger -->
```

## Key npm Packages (Angular)

```json
{
  "@microsoft/signalr": "latest",
  "@angular/material": "latest"
}
```

## Key Unity Packages

- **SignalR Client**: `Microsoft.AspNetCore.SignalR.Client` via NuGetForUnity
- **GLB Loading**: UnityGLTF (`com.unity.cloud.gltfast`) — built into Unity 6
- **UI Toolkit**: Built-in Unity 6

---

## Важные ограничения и решения

| Проблема | Решение |
|----------|---------|
|                             |                                                      |
| LiDAR scan export format | 3D Scanner App (iPad) экспортирует .obj, .ply, .usdz |
| Сегментация объектов в куче | MVP: по material/group из .obj. Future: SAM |
| Большие 3D файлы (100MB+) | Chunked upload, mesh simplification (MeshLab CLI) |
| SignalR в Unity main thread | Dispatcher pattern (UnityMainThread.Enqueue) |
| Real-time в Unity WebGL | SignalR fallback to Long Polling |
