using System;
using System.Collections.Generic;
using HomeInventory3D.Networking;
using UnityEngine;

namespace HomeInventory3D.Scene
{
    /// <summary>
    /// Manages the virtual container (chest) in the scene.
    /// Tracks spawned items and container metadata.
    /// </summary>
    public class ContainerManager : MonoBehaviour
    {
        [SerializeField] private Transform itemsParent;
        [SerializeField] private Vector3 containerDimensions = Vector3.one;

        private readonly Dictionary<string, ItemController> _spawnedItems = new();
        private ContainerDto _containerData;

        /// <summary>Currently loaded container ID.</summary>
        public string ContainerId => _containerData?.id;

        /// <summary>All spawned item controllers.</summary>
        public IReadOnlyDictionary<string, ItemController> SpawnedItems => _spawnedItems;

        /// <summary>Parent transform for spawned items.</summary>
        public Transform ItemsParent => itemsParent != null ? itemsParent : transform;

        /// <summary>
        /// Initializes the container with backend data.
        /// </summary>
        public void Initialize(ContainerDto container)
        {
            _containerData = container;

            if (container.widthMm.HasValue && container.heightMm.HasValue && container.depthMm.HasValue)
            {
                containerDimensions = new Vector3(
                    container.widthMm.Value / 1000f,
                    container.heightMm.Value / 1000f,
                    container.depthMm.Value / 1000f);
            }

            gameObject.name = $"Container_{container.name}";
        }

        /// <summary>
        /// Converts relative position (0-1) to world position inside the container.
        /// </summary>
        public Vector3 RelativeToWorldPosition(float? x, float? y, float? z)
        {
            var half = containerDimensions * 0.5f;
            return ItemsParent.position + new Vector3(
                (x ?? 0.5f) * containerDimensions.x - half.x,
                (y ?? 0f) * containerDimensions.y,
                (z ?? 0.5f) * containerDimensions.z - half.z);
        }

        /// <summary>
        /// Registers a spawned item.
        /// </summary>
        public void RegisterItem(ItemController item)
        {
            if (item.ItemId != null)
                _spawnedItems[item.ItemId] = item;
        }

        /// <summary>
        /// Removes an item by ID.
        /// </summary>
        public bool RemoveItem(string itemId)
        {
            if (!_spawnedItems.TryGetValue(itemId, out var item))
                return false;

            _spawnedItems.Remove(itemId);
            Destroy(item.gameObject);
            return true;
        }

        /// <summary>
        /// Clears all spawned items.
        /// </summary>
        public void ClearItems()
        {
            foreach (var item in _spawnedItems.Values)
            {
                if (item != null)
                    Destroy(item.gameObject);
            }

            _spawnedItems.Clear();
        }
    }
}
