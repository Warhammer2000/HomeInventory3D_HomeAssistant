"""Webhook handlers for Yandex Алиса and Google Home."""

import logging
from aiohttp import web

from .api_client import HomeInventoryApiClient

_LOGGER = logging.getLogger(__name__)


async def async_handle_alice_webhook(
    client: HomeInventoryApiClient, hass, webhook_id: str, request: web.Request
) -> web.Response:
    """Handle Yandex Алиса webhook.

    Алиса sends: {"request": {"command": "где лежат булавки"}, ...}
    We return: {"response": {"text": "...", "tts": "..."}, ...}
    """
    try:
        data = await request.json()
        command = data.get("request", {}).get("command", "")
        session = data.get("session", {})

        _LOGGER.info("Алиса query: '%s'", command)

        if not command:
            answer = "Скажите, что хотите найти. Например: где лежит отвёртка?"
        else:
            result = await client.voice_search_and_notify(command)
            answer = result.get("answer", "Не удалось найти.")

        return web.json_response({
            "response": {
                "text": answer,
                "tts": answer,
                "end_session": False,
            },
            "session": {
                "session_id": session.get("session_id", ""),
                "message_id": session.get("message_id", 0),
                "user_id": session.get("user_id", ""),
            },
            "version": "1.0",
        })
    except Exception as err:
        _LOGGER.error("Alice webhook error: %s", err)
        return web.json_response({
            "response": {
                "text": "Произошла ошибка. Попробуйте позже.",
                "tts": "Произошла ошибка. Попробуйте позже.",
                "end_session": False,
            },
            "version": "1.0",
        })


async def async_handle_google_webhook(
    client: HomeInventoryApiClient, hass, webhook_id: str, request: web.Request
) -> web.Response:
    """Handle Google Home / Google Actions webhook.

    Simplified handler for Google Assistant fulfillment.
    """
    try:
        data = await request.json()

        # Extract query from Google Actions format
        query_result = data.get("queryResult", {})
        query_text = query_result.get("queryText", "")

        _LOGGER.info("Google Home query: '%s'", query_text)

        if not query_text:
            answer = "What are you looking for?"
        else:
            result = await client.voice_search_and_notify(query_text)
            answer = result.get("answer", "Item not found.")

        return web.json_response({
            "fulfillmentText": answer,
            "fulfillmentMessages": [{"text": {"text": [answer]}}],
        })
    except Exception as err:
        _LOGGER.error("Google webhook error: %s", err)
        return web.json_response({
            "fulfillmentText": "An error occurred. Please try again.",
        })
