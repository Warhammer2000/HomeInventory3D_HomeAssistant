using System;
using System.Collections;
using HomeInventory3D.Networking;
using UnityEngine;

namespace HomeInventory3D.Scene
{
    /// <summary>
    /// Per-object behavior: hover scale + events for HUD.
    /// </summary>
    public class ItemController : MonoBehaviour
    {
        [SerializeField] private string itemId;
        [SerializeField] private string itemName;
        [SerializeField] private string[] tags;
        [SerializeField] private float confidence;
        [SerializeField] private string createdAt;
        [SerializeField] private string thumbnailUrl;
        [SerializeField] private string materialType;

        [Header("Hover Settings")]
        [SerializeField] private float hoverScale = 1.3f;
        [SerializeField] private float hoverSpeed = 8f;
        [SerializeField] private float floatAmplitude = 0.01f;
        [SerializeField] private float floatSpeed = 2f;

        private Vector3 _baseScale;
        private Vector3 _basePosition;
        private bool _isHovered;
        private bool _scaleReady;

        public event Action<ItemController> OnSelected;
        public event Action<ItemController> OnHoverEnter;
        public event Action<ItemController> OnHoverExit;

        public string ItemId => itemId;
        public string ItemName => itemName;
        public string[] Tags => tags;
        public float Confidence => confidence;
        public string CreatedAt => createdAt;
        public string ThumbnailUrl => thumbnailUrl;
        public string MaterialType => materialType;
        public bool IsHovered => _isHovered;

        public void Initialize(ItemDto dto)
        {
            itemId = dto.id;
            itemName = dto.name;
            tags = dto.tags ?? Array.Empty<string>();
            confidence = dto.confidence ?? 0f;
            createdAt = dto.createdAt;
            thumbnailUrl = dto.thumbnailPath;
            gameObject.name = $"Item_{dto.name}_{dto.id[..8]}";
        }

        public void Initialize(ItemAddedEvent evt)
        {
            itemId = evt.id;
            itemName = evt.name;
            tags = evt.tags ?? Array.Empty<string>();
            confidence = evt.confidence ?? 0f;
            thumbnailUrl = evt.thumbnailUrl;
            materialType = evt.materialType;
            gameObject.name = $"Item_{evt.name}_{evt.id[..8]}";
        }

        private void Start()
        {
            StartCoroutine(CaptureBaseTransform());
        }

        private IEnumerator CaptureBaseTransform()
        {
            yield return new WaitForSeconds(1.5f);
            _baseScale = transform.localScale;
            _basePosition = transform.localPosition;
            _scaleReady = true;
        }

        private void Update()
        {
            if (!_scaleReady) return;

            var targetScale = _isHovered ? _baseScale * hoverScale : _baseScale;
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * hoverSpeed);

            if (_isHovered)
            {
                var floatY = Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
                var pos = _basePosition;
                pos.y += floatY;
                transform.localPosition = Vector3.Lerp(transform.localPosition, pos, Time.deltaTime * hoverSpeed);
            }
        }

        private void OnMouseEnter()
        {
            _isHovered = true;
            OnHoverEnter?.Invoke(this);
        }

        private void OnMouseExit()
        {
            _isHovered = false;
            OnHoverExit?.Invoke(this);
        }

        private void OnMouseDown()
        {
            OnSelected?.Invoke(this);
        }
    }
}
