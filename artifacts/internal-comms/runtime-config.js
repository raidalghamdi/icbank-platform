/*
 * Deployed-environment configuration.
 *
 * Keep this file ahead of each page's inline application script: apiUrl() reads
 * window.__API_BASE__ while it is being initialised. The API uses a different
 * origin from the static frontend in development, so a relative /api request
 * would otherwise be sent to the frontend host and return its HTML shell.
 */
window.__API_BASE__ = 'https://icbank-dev-api.azurewebsites.net';
window.__API_VERSION__ = 'v1';
