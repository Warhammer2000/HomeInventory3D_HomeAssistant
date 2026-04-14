# HomeInventory3D — Настройка голосовых ассистентов

## Архитектура

```
"Алиса, где лежат булавки?"
    → Яндекс Dialogs → Home Assistant webhook
    → HA вызывает POST /api/voice/search-and-notify
    → Backend: pg_trgm поиск → текстовый ответ + SignalR broadcast
    → Алиса озвучивает ответ
    → Unity получает SignalR event → камера летит к предмету → подсветка
```

---

## 1. Установка Home Assistant компонента

### 1.1. Скопировать компонент

Скопируйте папку `custom_components/home_inventory_3d/` в директорию Home Assistant:

```bash
cp -r homeassistant/custom_components/home_inventory_3d/ \
  /config/custom_components/home_inventory_3d/
```

> `/config/` — корневая директория конфигурации Home Assistant.  
> В Docker: обычно `/homeassistant/` или монтированный volume.

### 1.2. Перезапустить Home Assistant

```bash
# Docker
docker restart homeassistant

# Supervisor
ha core restart

# HAOS
Settings → System → Restart
```

### 1.3. Добавить интеграцию

1. Откройте Home Assistant → **Settings → Devices & Services → Add Integration**
2. Найдите **HomeInventory3D**
3. Введите URL бэкенда: `http://<ваш-IP>:5300`
4. Нажмите **Submit**

После добавления в логах HA появится:
```
HomeInventory3D registered. Backend: http://...:5300
Alice webhook: /api/webhook/home_inventory_3d_alice
Google webhook: /api/webhook/home_inventory_3d_google
```

---

## 2. Подключение Яндекс Алисы

### 2.1. Создать навык в Яндекс Dialogs

1. Откройте [dialogs.yandex.ru](https://dialogs.yandex.ru/)
2. Нажмите **Создать навык** → **Навык в Алисе**
3. Заполните:
   - **Название**: HomeInventory (или любое другое)
   - **Активационное имя**: Инвентарь (или "Мой ящик", "Склад")
   - **Webhook URL**: `https://<ваш-HA-домен>/api/webhook/home_inventory_3d_alice`
4. Сохраните

> ⚠️ Webhook URL должен быть доступен из интернета (HTTPS). Используйте:
> - Nabu Casa (Home Assistant Cloud) — самый простой
> - Nginx reverse proxy + Let's Encrypt
> - Cloudflare Tunnel

### 2.2. Использование

```
"Алиса, попроси Инвентарь где лежат булавки"
→ "Булавки находятся в контейнере Швейный набор, полка 3."
```

```
"Алиса, спроси Инвентарь найти отвёртку"
→ "Отвёртка крестовая PH2 находится в контейнере Ящик с инструментами, Гараж полка 2."
```

---

## 3. Подключение Google Home

### 3.1. Создать Actions проект

1. Откройте [Actions Console](https://console.actions.google.com/)
2. **New project** → название → **Custom** → **Conversational**
3. В разделе **Fulfillment**:
   - Включите **Webhook**
   - URL: `https://<ваш-HA-домен>/api/webhook/home_inventory_3d_google`
4. Создайте Intent:
   - **Training phrases**: "where is the screwdriver", "find my keys", "где лежит отвёртка"
   - **Action**: `search.inventory`
   - **Fulfillment**: Enable webhook

### 3.2. Использование

```
"Hey Google, ask Home Inventory where is the screwdriver"
→ "Screwdriver is in container Tool Box, Garage shelf 2."
```

---

## 4. Подключение Apple HomeKit / Siri

Apple не поддерживает кастомные голосовые навыки напрямую. Варианты:

### Вариант A: Siri Shortcuts (рекомендуется)

1. Откройте **Shortcuts** на iPhone
2. Создайте новый Shortcut:
   - **Ask for Input** → "Что найти?"
   - **Get Contents of URL**:
     - URL: `http://<ваш-IP>:5300/api/voice/search-and-notify`
     - Method: POST
     - Headers: `Content-Type: application/json`
     - Body: `{"query": "<input>"}`
   - **Get Dictionary Value** → ключ `answer`
   - **Speak Text** → результат
3. Назовите: "Найди в инвентаре"
4. Используйте: **"Hey Siri, найди в инвентаре"**

### Вариант B: Через Home Assistant

Если HA подключен к HomeKit через интеграцию, можно создать автоматизацию:
1. HA → **Settings → Automations → Create**
2. Trigger: HA conversation API
3. Action: REST command к `/api/voice/search-and-notify`

---

## 5. Прямой API доступ (без голосовых ассистентов)

### Поиск (без уведомлений)
```bash
curl http://localhost:5300/api/voice/search?q=отвёртка
```

### Поиск + уведомление в Unity
```bash
curl -X POST http://localhost:5300/api/voice/search-and-notify \
  -H "Content-Type: application/json" \
  -d '{"query":"отвёртка"}'
```

Ответ:
```json
{
  "answer": "«Отвёртка крестовая» находится в контейнере «Ящик», Гараж полка 2.",
  "items": [
    {
      "id": "019d...",
      "containerId": "019d...",
      "name": "Отвёртка крестовая",
      "containerName": "Ящик",
      "containerLocation": "Гараж полка 2"
    }
  ]
}
```

При вызове `search-and-notify`:
- Бэкенд отправляет SignalR event `VoiceSearchResult`
- Unity камера автоматически летит к найденному предмету
- Предмет подсвечивается на 8 секунд

---

## 6. Что происходит в Unity

При получении SignalR события `VoiceSearchResult`:

1. **VoiceSearchHandler** проверяет — загружен ли нужный контейнер?
2. Если нет → загружает сцену контейнера через `SceneLoader`
3. Камера плавно перелетает к предмету через `OrbitCamera.FlyTo()`
4. Предмет подсвечивается пульсирующим highlight эффектом
5. Toast уведомление показывает текстовый ответ
6. Через 8 секунд highlight автоматически выключается

---

## 7. Устранение неполадок

| Проблема | Решение |
|----------|---------|
| Алиса не отвечает | Проверьте webhook URL, HTTPS, доступность HA из интернета |
| "Не нашёл" хотя предмет есть | Попробуйте другую формулировку, поиск нечёткий (pg_trgm) |
| Unity не реагирует | Проверьте SignalR подключение в Unity Console |
| HA не находит интеграцию | Перезапустите HA, проверьте что папка `custom_components/home_inventory_3d/` скопирована |
| 401 от бэкенда | CORS — добавьте URL Home Assistant в `Cors:Origins` в `appsettings.json` |

---

## 8. Структура файлов компонента

```
custom_components/home_inventory_3d/
├── __init__.py         # Точка входа: регистрация webhooks
├── manifest.json       # Метаданные компонента
├── config_flow.py      # UI: ввод backend URL
├── const.py            # Константы (DOMAIN, webhook IDs)
├── webhook.py          # Обработчики: Алиса + Google
├── api_client.py       # HTTP клиент к бэкенду
├── strings.json        # Локализация
└── translations/
    ├── en.json
    └── ru.json
```
