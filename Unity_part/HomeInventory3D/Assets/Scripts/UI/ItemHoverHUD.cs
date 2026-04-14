using HomeInventory3D.Scene;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HomeInventory3D.UI
{
    /// <summary>
    /// Shows a 3D info card above hovered items.
    /// Card stays constant screen size regardless of camera distance.
    /// </summary>
    public class ItemHoverHUD : MonoBehaviour
    {
        [SerializeField] private ContainerManager containerManager;

        private ItemController _currentItem;
        private GameObject _card;
        private Camera _mainCam;

        private void Start()
        {
            _mainCam = Camera.main;
            if (containerManager == null)
                containerManager = FindAnyObjectByType<ContainerManager>();
        }

        private void Update()
        {
            if (_mainCam == null) return;

            var mouse = Mouse.current;
            if (mouse == null) return;

            var ray = _mainCam.ScreenPointToRay(mouse.position.ReadValue());
            if (Physics.Raycast(ray, out var hit, 100f))
            {
                var item = hit.collider.GetComponentInParent<ItemController>();
                if (item == null) item = hit.transform.GetComponentInParent<ItemController>();

                if (item != null && item != _currentItem)
                {
                    HideCard();
                    _currentItem = item;
                    ShowCard(item);
                }
                else if (item == null && _currentItem != null)
                {
                    HideCard();
                }
            }
            else if (_currentItem != null)
            {
                HideCard();
            }

            if (_card == null) return;

            // Billboard — face camera
            _card.transform.LookAt(_mainCam.transform);
            _card.transform.Rotate(0, 180, 0);

            // Fixed small scale — always readable, never huge
            _card.transform.localScale = Vector3.Lerp(
                _card.transform.localScale, Vector3.one * 0.08f, Time.deltaTime * 15f);
        }

        private void ShowCard(ItemController item)
        {
            _card = new GameObject($"HUD_{item.ItemName}");
            _card.transform.position = item.transform.position + Vector3.up * 0.25f;

            // Just the name
            MakeText(_card.transform, item.ItemName ?? "???",
                Vector3.zero, 0.03f, Color.white, FontStyle.Bold);

            // Start tiny for pop animation
            _card.transform.localScale = Vector3.zero;
        }

        private void HideCard()
        {
            if (_card != null)
            {
                UnityEngine.Object.Destroy(_card);
                _card = null;
            }
            _currentItem = null;
        }

        private static void MakeText(Transform parent, string text, Vector3 localPos,
            float charSize, Color color, FontStyle style)
        {
            var go = new GameObject("T");
            go.transform.SetParent(parent);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            var tm = go.AddComponent<TextMesh>();
            tm.text = text;
            tm.characterSize = charSize;
            tm.fontSize = 200;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
            tm.fontStyle = style;

            go.GetComponent<MeshRenderer>().sortingOrder = 200;
        }

        private void OnDestroy()
        {
            HideCard();
        }
    }
}
