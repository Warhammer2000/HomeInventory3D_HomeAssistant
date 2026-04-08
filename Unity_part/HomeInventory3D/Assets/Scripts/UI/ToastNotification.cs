using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace HomeInventory3D.UI
{
    /// <summary>
    /// Displays brief toast notifications ("Отвёртка добавлена!").
    /// Uses UI Toolkit.
    /// </summary>
    public class ToastNotification : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private float displayDuration = 3f;
        [SerializeField] private float fadeDuration = 0.5f;

        private VisualElement _toastContainer;

        private void Start()
        {
            if (uiDocument == null) return;

            _toastContainer = uiDocument.rootVisualElement.Q<VisualElement>("toast-container");
            if (_toastContainer == null)
            {
                _toastContainer = new VisualElement { name = "toast-container" };
                _toastContainer.style.position = Position.Absolute;
                _toastContainer.style.bottom = 20;
                _toastContainer.style.right = 20;
                _toastContainer.style.width = 300;
                uiDocument.rootVisualElement.Add(_toastContainer);
            }
        }

        /// <summary>
        /// Shows a toast notification message.
        /// </summary>
        public void Show(string message)
        {
            if (_toastContainer == null) return;

            var toast = new Label(message);
            toast.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            toast.style.color = Color.white;
            toast.style.paddingTop = 10;
            toast.style.paddingBottom = 10;
            toast.style.paddingLeft = 16;
            toast.style.paddingRight = 16;
            toast.style.marginBottom = 8;
            toast.style.borderTopLeftRadius = 8;
            toast.style.borderTopRightRadius = 8;
            toast.style.borderBottomLeftRadius = 8;
            toast.style.borderBottomRightRadius = 8;
            toast.style.fontSize = 14;

            _toastContainer.Add(toast);
            StartCoroutine(AutoRemove(toast));
        }

        private IEnumerator AutoRemove(VisualElement toast)
        {
            yield return new WaitForSeconds(displayDuration);

            var elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                toast.style.opacity = 1f - elapsed / fadeDuration;
                elapsed += Time.deltaTime;
                yield return null;
            }

            toast.RemoveFromHierarchy();
        }
    }
}
