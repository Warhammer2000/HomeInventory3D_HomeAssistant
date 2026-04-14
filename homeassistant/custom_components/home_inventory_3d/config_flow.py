"""Config flow for HomeInventory3D integration."""

import voluptuous as vol
from homeassistant import config_entries

from .const import DOMAIN, CONF_BACKEND_URL, DEFAULT_BACKEND_URL


class HomeInventory3DConfigFlow(config_entries.ConfigFlow, domain=DOMAIN):
    """Handle a config flow for HomeInventory3D."""

    VERSION = 1

    async def async_step_user(self, user_input=None):
        """Handle the initial step — user enters backend URL."""
        if user_input is not None:
            return self.async_create_entry(
                title="HomeInventory3D",
                data=user_input,
            )

        return self.async_show_form(
            step_id="user",
            data_schema=vol.Schema({
                vol.Required(CONF_BACKEND_URL, default=DEFAULT_BACKEND_URL): str,
            }),
        )
