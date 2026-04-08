using UnityEngine;

namespace HomeInventory3D.Networking
{
    /// <summary>
    /// Centralized API configuration. Attach to a persistent GameObject.
    /// </summary>
    public class ApiConfig : MonoBehaviour
    {
        [SerializeField] private string backendUrl = "http://localhost:5000";

        /// <summary>
        /// Singleton instance.
        /// </summary>
        public static ApiConfig Instance { get; private set; }

        /// <summary>
        /// Base URL of the backend API (no trailing slash).
        /// </summary>
        public string BackendUrl => backendUrl.TrimEnd('/');

        /// <summary>
        /// SignalR hub URL.
        /// </summary>
        public string HubUrl => $"{BackendUrl}/hubs/inventory";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
}
