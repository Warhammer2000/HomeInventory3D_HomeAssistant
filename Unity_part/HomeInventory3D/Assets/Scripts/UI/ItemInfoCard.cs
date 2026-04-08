using HomeInventory3D.Scene;
using UnityEngine;
using UnityEngine.UIElements;

namespace HomeInventory3D.UI
{
    /// <summary>
    /// Displays item details popup when an item is selected.
    /// Uses UI Toolkit.
    /// </summary>
    public class ItemInfoCard : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;

        private VisualElement _card;
        private Label _nameLabel;
        private Label _tagsLabel;
        private Label _confidenceLabel;

        private void Start()
        {
            if (uiDocument == null) return;

            BuildUI();
            Hide();
        }

        /// <summary>
        /// Shows the info card for a selected item.
        /// </summary>
        public void Show(ItemController item)
        {
            if (_card == null) return;

            _nameLabel.text = item.ItemName;
            _tagsLabel.text = item.Tags != null && item.Tags.Length > 0
                ? string.Join(", ", item.Tags)
                : "No tags";
            _confidenceLabel.text = $"Confidence: {item.Confidence:P0}";

            _card.style.display = DisplayStyle.Flex;
        }

        /// <summary>
        /// Hides the info card.
        /// </summary>
        public void Hide()
        {
            if (_card != null)
                _card.style.display = DisplayStyle.None;
        }

        private void BuildUI()
        {
            var root = uiDocument.rootVisualElement;

            _card = new VisualElement { name = "item-info-card" };
            _card.style.position = Position.Absolute;
            _card.style.top = 20;
            _card.style.left = 20;
            _card.style.backgroundColor = new Color(0.12f, 0.12f, 0.12f, 0.95f);
            _card.style.paddingTop = _card.style.paddingBottom = 16;
            _card.style.paddingLeft = _card.style.paddingRight = 20;
            _card.style.borderTopLeftRadius = _card.style.borderTopRightRadius = 10;
            _card.style.borderBottomLeftRadius = _card.style.borderBottomRightRadius = 10;
            _card.style.width = 260;

            _nameLabel = new Label("Item");
            _nameLabel.style.color = Color.white;
            _nameLabel.style.fontSize = 18;
            _nameLabel.style.marginBottom = 8;
            _nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

            _tagsLabel = new Label("Tags");
            _tagsLabel.style.color = new Color(0.7f, 0.7f, 0.7f, 1f);
            _tagsLabel.style.fontSize = 13;
            _tagsLabel.style.marginBottom = 4;

            _confidenceLabel = new Label("Confidence");
            _confidenceLabel.style.color = new Color(0.5f, 0.8f, 1f, 1f);
            _confidenceLabel.style.fontSize = 12;

            _card.Add(_nameLabel);
            _card.Add(_tagsLabel);
            _card.Add(_confidenceLabel);
            root.Add(_card);
        }
    }
}
