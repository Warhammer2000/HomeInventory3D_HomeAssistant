using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace HomeInventory3D.Networking
{
    /// <summary>
    /// HTTP REST client for the backend API using UnityWebRequest.
    /// </summary>
    public class ApiClient
    {
        private readonly string _baseUrl;

        /// <summary>Base URL of the backend API.</summary>
        public string BaseUrl => _baseUrl;

        public ApiClient(string baseUrl)
        {
            _baseUrl = baseUrl.TrimEnd('/');
        }

        /// <summary>
        /// Loads the full 3D scene for a container (mesh + items).
        /// </summary>
        public async Task<SceneDto> GetSceneAsync(Guid containerId)
        {
            var url = $"{_baseUrl}/api/containers/{containerId}/scene";
            return await GetAsync<SceneDto>(url);
        }

        /// <summary>
        /// Gets a container by ID.
        /// </summary>
        public async Task<ContainerDto> GetContainerAsync(Guid containerId)
        {
            var url = $"{_baseUrl}/api/containers/{containerId}";
            return await GetAsync<ContainerDto>(url);
        }

        /// <summary>
        /// Gets all containers.
        /// </summary>
        public async Task<ContainerDto[]> GetAllContainersAsync()
        {
            var url = $"{_baseUrl}/api/containers";
            return await GetAsync<ContainerDto[]>(url);
        }

        /// <summary>
        /// Searches items by query.
        /// </summary>
        public async Task<ItemDto[]> SearchItemsAsync(string query, int limit = 20)
        {
            var url = $"{_baseUrl}/api/items/search?q={UnityWebRequest.EscapeURL(query)}&limit={limit}";
            return await GetAsync<ItemDto[]>(url);
        }

        /// <summary>
        /// Gets items for a container.
        /// </summary>
        public async Task<ItemDto[]> GetItemsAsync(Guid containerId)
        {
            var url = $"{_baseUrl}/api/items?containerId={containerId}";
            return await GetAsync<ItemDto[]>(url);
        }

        /// <summary>
        /// Downloads raw bytes from a URL (for mesh/texture loading).
        /// </summary>
        public async Task<byte[]> DownloadBytesAsync(string url)
        {
            if (!url.StartsWith("http"))
                url = $"{_baseUrl}/{url.TrimStart('/')}";

            using var request = UnityWebRequest.Get(url);
            var operation = request.SendWebRequest();

            while (!operation.isDone)
                await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Download failed: {url} — {request.error}");
                return null;
            }

            return request.downloadHandler.data;
        }

        private async Task<T> GetAsync<T>(string url)
        {
            using var request = UnityWebRequest.Get(url);
            request.SetRequestHeader("Accept", "application/json");

            var operation = request.SendWebRequest();

            while (!operation.isDone)
                await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"API request failed: {url} — {request.error}");
                return default;
            }

            var json = request.downloadHandler.text;
            return JsonUtility.FromJson<T>(json);
        }
    }
}
