using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Unity.AppUI.UI;
using Unity.Properties;

using UnityEngine;
using UnityEngine.UIElements;

namespace eu.foodmission.platform.Components
{
    /// <summary>
    /// Search field that filters candidates through <see cref="SearchAsync"/> and lists them below the field.
    /// Candidates are pantry items today; the delegate is the seam for other sources (non-pantry products) later.
    /// </summary>
    [UxmlElement]
    public partial class FMPantryPickerField : VisualElement
    {
        private const int DebounceMs = 250;

        private readonly Unity.AppUI.UI.TextField _textField;
        private readonly VisualElement _resultsContainer;
        private CancellationTokenSource _debounceCts;
        private bool _popoverVisible;

        [UxmlAttribute("placeholder")]
        [CreateProperty]
        public string Placeholder
        {
            get => _textField?.placeholder ?? "";
            set
            {
                if (_textField != null)
                {
                    _textField.placeholder = value;
                }
            }
        }

        public string NoResultsText { get; set; } = "";

        public Func<string, Task<List<PantryItemView>>> SearchAsync { get; set; }

        /// <summary>Row text for a candidate. Defaults to its display name.</summary>
        public Func<PantryItemView, string> FormatItem { get; set; }

        public event Action<PantryItemView> OnItemSelected;
        public event Action<bool> OnPopoverVisibilityChanged;

        public VisualElement InputElement => _textField;

        public FMPantryPickerField()
        {
            AddToClassList("fm-ppf");

            _textField = new Unity.AppUI.UI.TextField();
            _textField.AddToClassList("fm-ppf-field");
            Add(_textField);

            _resultsContainer = new VisualElement();
            _resultsContainer.AddToClassList("fm-scf-results");
            _resultsContainer.AddToClassList("fm-ppf-results");
            Add(_resultsContainer);

            // AppUI TextField only notifies on Enter/blur; listen to the inner field for keystrokes.
            _textField.schedule.Execute(() =>
            {
                var innerField = _textField.Q<UnityEngine.UIElements.TextField>();
                if (innerField != null)
                {
                    innerField.RegisterValueChangedCallback(OnInnerValueChanged);
                }
            }).ExecuteLater(0);

            RegisterCallback<DetachFromPanelEvent>(_ => CancelPendingSearch());
        }

        public void ClearSearch()
        {
            CancelPendingSearch();
            _textField.value = "";
            _resultsContainer.Clear();
            SetPopoverVisible(false);
        }

        private void OnInnerValueChanged(ChangeEvent<string> evt)
        {
            CancelPendingSearch();

            if (string.IsNullOrWhiteSpace(evt.newValue))
            {
                _resultsContainer.Clear();
                SetPopoverVisible(false);
                return;
            }

            _debounceCts = new CancellationTokenSource();
            _ = DebouncedSearchAsync(evt.newValue, _debounceCts.Token);
        }

        private async Task DebouncedSearchAsync(string query, CancellationToken ct)
        {
            try
            {
                await Task.Delay(DebounceMs, ct);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (ct.IsCancellationRequested || SearchAsync == null)
            {
                return;
            }

            List<PantryItemView> results;
            try
            {
                results = await SearchAsync(query) ?? new List<PantryItemView>();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FMPantryPickerField] Search failed: {ex.Message}");
                return;
            }

            if (ct.IsCancellationRequested)
            {
                return;
            }

            ShowResults(results);
        }

        private void ShowResults(List<PantryItemView> results)
        {
            _resultsContainer.Clear();

            if (results.Count == 0)
            {
                var noResults = new Text { text = NoResultsText };
                noResults.AddToClassList("fm-scf-no-results");
                _resultsContainer.Add(noResults);
            }
            else
            {
                foreach (PantryItemView view in results)
                {
                    PantryItemView captured = view;
                    var row = new Unity.AppUI.UI.Button
                    {
                        quiet = true,
                        size = Size.M,
                        title = FormatItem != null ? FormatItem(captured) : captured.DisplayName
                    };
                    row.AddToClassList("fm-scf-result-row");
                    row.AddToClassList("fm-button-align-left");
                    row.AddToClassList("fm-ppf-result-row");
                    row.clicked += () => Select(captured);
                    _resultsContainer.Add(row);
                }
            }

            SetPopoverVisible(true);
        }

        private void Select(PantryItemView view)
        {
            ClearSearch();
            OnItemSelected?.Invoke(view);
        }

        private void SetPopoverVisible(bool visible)
        {
            _resultsContainer.EnableInClassList("visible", visible);
            if (_popoverVisible == visible)
            {
                return;
            }
            _popoverVisible = visible;
            OnPopoverVisibilityChanged?.Invoke(visible);
        }

        private void CancelPendingSearch()
        {
            _debounceCts?.Cancel();
            _debounceCts?.Dispose();
            _debounceCts = null;
        }
    }
}
