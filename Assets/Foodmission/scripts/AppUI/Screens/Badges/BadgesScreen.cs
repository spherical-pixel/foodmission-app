using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Threading.Tasks;

using eu.foodmission.platform.Components;

using Unity.AppUI.Core;
using Unity.AppUI.MVVM;
using Unity.AppUI.UI;

using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.Localization.Settings;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace eu.foodmission.platform
{
    [Preserve]
    class BadgesScreen : NavigationScreenBase<BadgesViewModel>
    {
        protected override bool ApplySafeAreaBottom => false;
        protected override bool ApplySafeAreaLeft => false;
        protected override bool ApplySafeAreaRight => false;
        protected override bool ApplySafeAreaTop => false;
        protected override bool IsFixedContent => false;

        private static IFormatProvider DateFormatter => LocalizationSettings.SelectedLocale?.Formatter ?? CultureInfo.CurrentCulture;

        private readonly Heading _title;
        private readonly Text _summary;
        private readonly VisualElement _grid;
        private readonly ISpriteService _spriteService;

        private readonly List<(VisualElement Card, BadgeItem Item)> _cards = new();
        private readonly List<AccessibilityNode> _cardNodes = new();

        public BadgesScreen()
        {
            InitializeComponent(App.current.services
                .GetRequiredService<ITemplateService>()
                .Get(TemplateAddresses.Badges));

            _title = contentContainer.Q<Heading>("screen-title");
            _summary = contentContainer.Q<Text>("summary");
            _grid = contentContainer.Q<VisualElement>("badges-grid");
            _spriteService = App.current.services.GetService<ISpriteService>();

            if (_title != null)
            {
                _title.text = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "BADGES_TITLE");
            }
        }

        protected override void OnViewModelBound()
        {
            base.OnViewModelBound();

            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            TrackLoadingOverlay(() => _viewModel.IsLoading, nameof(BadgesViewModel.IsLoading));
            Rebuild();

            _ = _viewModel.LoadBadgesAsync().ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    Debug.LogError($"[{GetType().Name}] LoadBadgesAsync failed: {t.Exception}");
                }
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        protected override void OnViewModelUnbinding()
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            base.OnViewModelUnbinding();
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(BadgesViewModel.Badges):
                    Rebuild();
                    break;
                case nameof(BadgesViewModel.ErrorDetail):
                    if (_viewModel.ErrorDetail != null)
                    {
                        FMDialog.ShowApiError(this, LocalizationSettings.StringDatabase.GetLocalizedString("UI", "ERROR_TITLE"), _viewModel.ErrorDetail);
                        _viewModel.ErrorDetail = null;
                    }
                    break;
            }
        }

        private void Rebuild()
        {
            if (_grid == null)
            {
                return;
            }

            _grid.Clear();
            _cards.Clear();

            if (_summary != null)
            {
                bool hasData = _viewModel.TotalCount > 0;
                _summary.EnableInClassList("hidden", !hasData);
                _summary.text = hasData
                    ? LocalizationSettings.StringDatabase.GetLocalizedString("UI", "BADGES_SUMMARY", new object[] { _viewModel.EarnedCount, _viewModel.TotalCount })
                    : "";
            }

            foreach (BadgeItem item in _viewModel.Badges ?? new List<BadgeItem>())
            {
                VisualElement card = BuildCard(item);
                _grid.Add(card);
                _cards.Add((card, item));
            }

            RefreshAccessibilityNodes();
        }

        private VisualElement BuildCard(BadgeItem item)
        {
            var card = new VisualElement();
            card.AddToClassList("fm-badge-card");
            card.EnableInClassList("fm-badge-card--locked", !item.Earned);

            var image = new VisualElement();
            image.AddToClassList("fm-badge-card__image");
            card.Add(image);
            if (_spriteService != null)
            {
                _ = _spriteService.BindBackgroundSprite(image, item.SpriteAddress);
            }

            var name = new Text { text = item.Name ?? item.Code };
            name.AddToClassList("fm-badge-card__name");
            card.Add(name);

            var description = new Text { text = item.Description ?? "" };
            description.AddToClassList("fm-badge-card__description");
            card.Add(description);

            if (item.Earned && item.EarnedAt.HasValue)
            {
                string date = item.EarnedAt.Value.ToLocalTime().ToString("d", DateFormatter);
                var meta = new Text { text = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "BADGES_EARNED_ON", new object[] { date }) };
                meta.AddToClassList("fm-badge-card__meta");
                card.Add(meta);
            }

            if (item.ShowProgress)
            {
                var progress = new LinearProgress { variant = Progress.Variant.Determinate, value = item.Progress };
                progress.AddToClassList("fm-badge-card__progress");
                card.Add(progress);
            }

            return card;
        }

        // --------------------------------------------------------------------
        // Accessibility
        // --------------------------------------------------------------------

        protected override void SetupAccessibilityNodes()
        {
            base.SetupAccessibilityNodes();
            RefreshAccessibilityNodes();
        }

        protected override void TeardownAccessibilityNodes()
        {
            _cardNodes.Clear();
            base.TeardownAccessibilityNodes();
        }

        private void RefreshAccessibilityNodes()
        {
            if (_accessibilityHierarchy == null)
            {
                return;
            }

            foreach (AccessibilityNode node in _cardNodes)
            {
                _accessibilityHierarchy.RemoveNode(node);
            }
            _cardNodes.Clear();

            string earnedLabel = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "BADGES_EARNED");
            string lockedLabel = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "BADGES_LOCKED");

            foreach (var (card, item) in _cards)
            {
                string state = item.Earned ? earnedLabel : $"{lockedLabel}, {Mathf.RoundToInt(item.Progress * 100)}%";
                AccessibilityNode node = _accessibilityHierarchy.AddNode($"{item.Name ?? item.Code}, {state}. {item.Description}");
                node.role = AccessibilityRole.StaticText;
                VisualElement target = card;
                node.frameGetter = () =>
                {
                    if (target.panel == null)
                    {
                        return Rect.zero;
                    }
                    var r = target.worldBound;
                    var s = target.panel.scaledPixelsPerPoint;
                    return new Rect(r.position * s, r.size * s);
                };
                _cardNodes.Add(node);
            }
        }
    }
}
