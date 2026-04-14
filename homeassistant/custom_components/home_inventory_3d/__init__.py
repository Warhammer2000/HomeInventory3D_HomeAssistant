"""HomeInventory3D integration for Home Assistant.

Registers webhooks for Yandex Алиса and Google Home to search inventory
and trigger Unity 3D camera navigation via SignalR.
"""

import logging
from functools import partial

from homeassistant.components import webhook
from homeassistant.config_entries import ConfigEntry
from homeassistant.core import HomeAssistant
from homeassistant.helpers.aiohttp_client import async_get_clientsession

from .api_client import HomeInventoryApiClient
from .const import DOMAIN, CONF_BACKEND_URL, ALICE_WEBHOOK_ID, GOOGLE_WEBHOOK_ID
from .webhook import async_handle_alice_webhook, async_handle_google_webhook

_LOGGER = logging.getLogger(__name__)


async def async_setup_entry(hass: HomeAssistant, entry: ConfigEntry) -> bool:
    """Set up HomeInventory3D from a config entry."""
    backend_url = entry.data[CONF_BACKEND_URL]
    session = async_get_clientsession(hass)
    client = HomeInventoryApiClient(session, backend_url)

    hass.data.setdefault(DOMAIN, {})
    hass.data[DOMAIN][entry.entry_id] = client

    # Register Yandex Алиса webhook
    webhook.async_register(
        hass,
        DOMAIN,
        "HomeInventory3D — Алиса",
        ALICE_WEBHOOK_ID,
        partial(async_handle_alice_webhook, client),
    )

    # Register Google Home webhook
    webhook.async_register(
        hass,
        DOMAIN,
        "HomeInventory3D — Google",
        GOOGLE_WEBHOOK_ID,
        partial(async_handle_google_webhook, client),
    )

    _LOGGER.info(
        "HomeInventory3D registered. Backend: %s, Alice webhook: /api/webhook/%s, Google webhook: /api/webhook/%s",
        backend_url, ALICE_WEBHOOK_ID, GOOGLE_WEBHOOK_ID,
    )

    return True


async def async_unload_entry(hass: HomeAssistant, entry: ConfigEntry) -> bool:
    """Unload a config entry."""
    webhook.async_unregister(hass, ALICE_WEBHOOK_ID)
    webhook.async_unregister(hass, GOOGLE_WEBHOOK_ID)
    hass.data[DOMAIN].pop(entry.entry_id)
    return True
