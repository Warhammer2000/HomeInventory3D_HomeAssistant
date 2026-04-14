"""API client for HomeInventory3D backend."""

import logging
from aiohttp import ClientSession

_LOGGER = logging.getLogger(__name__)


class HomeInventoryApiClient:
    """Async HTTP client for the HomeInventory3D backend API."""

    def __init__(self, session: ClientSession, base_url: str) -> None:
        self._session = session
        self._base_url = base_url.rstrip("/")

    async def voice_search_and_notify(self, query: str) -> dict:
        """Search for an item and broadcast to Unity via SignalR.

        Args:
            query: The search query (e.g., "булавки")

        Returns:
            dict with 'answer' (str) and 'items' (list)
        """
        url = f"{self._base_url}/api/voice/search-and-notify"
        try:
            async with self._session.post(url, json={"query": query}) as resp:
                if resp.status == 200:
                    return await resp.json()
                _LOGGER.error("Backend returned %s: %s", resp.status, await resp.text())
                return {"answer": "Ошибка при поиске. Попробуйте позже.", "items": []}
        except Exception as err:
            _LOGGER.error("Failed to connect to backend: %s", err)
            return {"answer": "Не удалось подключиться к системе инвентаря.", "items": []}
