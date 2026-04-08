using UnityEngine;
using UnityEngine.UIElements;

namespace HomeInventory3D.UI
{
    /// <summary>
    /// Displays scan processing progress overlay.
    /// Uses UI Toolkit.
    /// </summary>
    public class ProgressOverlay : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;

        private VisualElement _overlay;
        private Label _stageLabel;
        private VisualElement _progressFill;
        private Label _percentLabel;

        private void Start()
        {
            if (uiDocument == null) return;

            BuildUI();
            Hide();
        }

        /// <summary>
        /// Shows the overlay with initial state.
        /// </summary>
        public void Show(string stage = "Starting...")
        {
            if (_overlay == null) return;

            _overlay.style.display = DisplayStyle.Flex;
            UpdateProgress(0, stage);
        }

        /// <summary>
        /// Updates progress bar and stage text.
        /// </summary>
        public void UpdateProgress(int percent, string stage)
        {
            if (_stageLabel != null) _stageLabel.text = stage;
            if (_percentLabel != null) _percentLabel.text = $"{percent}%";
            if (_progressFill != null) _progressFill.style.width = Length.Percent(percent);
        }

        /// <summary>
        /// Hides the overlay.
        /// </summary>
        public void Hide()
        {
            if (_overlay != null)
                _overlay.style.display = DisplayStyle.None;
        }

        private void BuildUI()
        {
            var root = uiDocument.rootVisualElement;

            _overlay = new VisualElement { name = "progress-overlay" };
            _overlay.style.position = Position.Absolute;
            _overlay.style.top = 0;
            _overlay.style.left = 0;
            _overlay.style.right = 0;
            _overlay.style.bottom = 0;
            _overlay.style.backgroundColor = new Color(0, 0, 0, 0.7f);
            _overlay.style.justifyContent = Justify.Center;
            _overlay.style.alignItems = Align.Center;

            var card = new VisualElement();
            card.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 1f);
            card.style.paddingTop = card.style.paddingBottom = 24;
            card.style.paddingLeft = card.style.paddingRight = 32;
            card.style.borderTopLeftRadius = card.style.borderTopRightRadius = 12;
            card.style.borderBottomLeftRadius = card.style.borderBottomRightRadius = 12;
            card.style.width = 320;

            _stageLabel = new Label("Processing...");
            _stageLabel.style.color = Color.white;
            _stageLabel.style.fontSize = 16;
            _stageLabel.style.marginBottom = 12;

            var progressBar = new VisualElement();
            progressBar.style.height = 8;
            progressBar.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 1f);
            progressBar.style.borderTopLeftRadius = progressBar.style.borderTopRightRadius = 4;
            progressBar.style.borderBottomLeftRadius = progressBar.style.borderBottomRightRadius = 4;

            _progressFill = new VisualElement();
            _progressFill.style.height = 8;
            _progressFill.style.backgroundColor = new Color(0.2f, 0.7f, 1f, 1f);
            _progressFill.style.borderTopLeftRadius = _progressFill.style.borderTopRightRadius = 4;
            _progressFill.style.borderBottomLeftRadius = _progressFill.style.borderBottomRightRadius = 4;
            _progressFill.style.width = Length.Percent(0);
            progressBar.Add(_progressFill);

            _percentLabel = new Label("0%");
            _percentLabel.style.color = new Color(0.7f, 0.7f, 0.7f, 1f);
            _percentLabel.style.fontSize = 12;
            _percentLabel.style.marginTop = 8;
            _percentLabel.style.unityTextAlign = TextAnchor.MiddleCenter;

            card.Add(_stageLabel);
            card.Add(progressBar);
            card.Add(_percentLabel);
            _overlay.Add(card);
            root.Add(_overlay);
        }
    }
}
