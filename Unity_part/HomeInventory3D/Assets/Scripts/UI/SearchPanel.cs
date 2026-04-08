using System;
using System.Collections.Generic;
using HomeInventory3D.Animation;
using HomeInventory3D.Networking;
using HomeInventory3D.Scene;
using UnityEngine;
using UnityEngine.UIElements;

namespace HomeInventory3D.UI
{
    /// <summary>
    /// Search bar + results panel. Highlights matching items in the scene.
    /// Uses UI Toolkit.
    /// </summary>
    public class SearchPanel : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private ContainerManager containerManager;
        [SerializeField] private Material highlightMaterial;

        private TextField _searchField;
        private VisualElement _resultsContainer;
        private ApiClient _apiClient;
        private readonly List<HighlightAnimation> _activeHighlights = new();

        private void Start()
        {
            if (ApiConfig.Instance != null)
                _apiClient = new ApiClient(ApiConfig.Instance.BackendUrl);

            if (uiDocument == null) return;

            BuildUI();
        }

        private async void PerformSearch(string query)
        {
            ClearHighlights();

            if (string.IsNullOrWhiteSpace(query) || _apiClient == null)
            {
                ClearResults();
                return;
            }

            var items = await _apiClient.SearchItemsAsync(query);
            if (items == null || items.Length == 0)
            {
                ShowNoResults();
                return;
            }

            _resultsContainer.Clear();

            foreach (var item in items)
            {
                var row = new Label($"{item.name} — {item.status}");
                row.style.color = Color.white;
                row.style.fontSize = 13;
                row.style.paddingTop = row.style.paddingBottom = 4;
                _resultsContainer.Add(row);

                // Highlight in scene if this item is spawned
                if (containerManager != null &&
                    containerManager.SpawnedItems.TryGetValue(item.id, out var controller))
                {
                    var highlight = controller.GetComponent<HighlightAnimation>();
                    if (highlight == null)
                        highlight = controller.gameObject.AddComponent<HighlightAnimation>();

                    highlight.StartHighlight(highlightMaterial);
                    _activeHighlights.Add(highlight);
                }
            }
        }

        private void ClearHighlights()
        {
            foreach (var h in _activeHighlights)
            {
                if (h != null)
                    h.StopHighlight();
            }

            _activeHighlights.Clear();
        }

        private void ClearResults()
        {
            _resultsContainer?.Clear();
        }

        private void ShowNoResults()
        {
            _resultsContainer?.Clear();
            var label = new Label("Nothing found");
            label.style.color = new Color(0.5f, 0.5f, 0.5f, 1f);
            label.style.fontSize = 13;
            _resultsContainer?.Add(label);
        }

        private void BuildUI()
        {
            var root = uiDocument.rootVisualElement;

            var panel = new VisualElement { name = "search-panel" };
            panel.style.position = Position.Absolute;
            panel.style.top = 20;
            panel.style.right = 20;
            panel.style.width = 280;

            _searchField = new TextField("Search");
            _searchField.style.marginBottom = 8;
            _searchField.RegisterValueChangedCallback(evt => PerformSearch(evt.newValue));

            _resultsContainer = new VisualElement { name = "search-results" };
            _resultsContainer.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            _resultsContainer.style.paddingTop = _resultsContainer.style.paddingBottom = 8;
            _resultsContainer.style.paddingLeft = _resultsContainer.style.paddingRight = 12;
            _resultsContainer.style.borderTopLeftRadius = _resultsContainer.style.borderTopRightRadius = 8;
            _resultsContainer.style.borderBottomLeftRadius = _resultsContainer.style.borderBottomRightRadius = 8;

            panel.Add(_searchField);
            panel.Add(_resultsContainer);
            root.Add(panel);
        }
    }
}
