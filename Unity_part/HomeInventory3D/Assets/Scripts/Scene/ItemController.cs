using System;
using HomeInventory3D.Networking;
using UnityEngine;

namespace HomeInventory3D.Scene
{
    /// <summary>
    /// Per-object behavior for inventory items in the scene.
    /// Handles hover, selection, and metadata display.
    /// </summary>
    public class ItemController : MonoBehaviour
    {
        [SerializeField] private string itemId;
        [SerializeField] private string itemName;
        [SerializeField] private string[] tags;
        [SerializeField] private float confidence;

        /// <summary>Fired when this item is selected (clicked).</summary>
        public event Action<ItemController> OnSelected;

        /// <summary>Unique ID from the backend.</summary>
        public string ItemId => itemId;

        /// <summary>Display name of the item.</summary>
        public string ItemName => itemName;

        /// <summary>Tags for search/filtering.</summary>
        public string[] Tags => tags;

        /// <summary>AI recognition confidence.</summary>
        public float Confidence => confidence;

        /// <summary>
        /// Initializes this controller with data from the backend.
        /// </summary>
        public void Initialize(ItemDto dto)
        {
            itemId = dto.id;
            itemName = dto.name;
            tags = dto.tags ?? Array.Empty<string>();
            confidence = dto.confidence ?? 0f;
            gameObject.name = $"Item_{dto.name}_{dto.id[..8]}";
        }

        /// <summary>
        /// Initializes from a SignalR ItemAdded event.
        /// </summary>
        public void Initialize(ItemAddedEvent evt)
        {
            itemId = evt.id;
            itemName = evt.name;
            tags = evt.tags ?? Array.Empty<string>();
            confidence = evt.confidence ?? 0f;
            gameObject.name = $"Item_{evt.name}_{evt.id[..8]}";
        }

        private void OnMouseDown()
        {
            OnSelected?.Invoke(this);
        }
    }
}
