using System;
using System.ComponentModel;
using eu.foodmission.platform.Components;
using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace eu.foodmission.platform
{
    [Preserve]
    public class KnowledgeScreen : NavigationScreenBase<KnowledgeViewModel>
    {
        protected override bool ApplySafeAreaBottom => false;
        protected override bool ApplySafeAreaLeft => false;
        protected override bool ApplySafeAreaRight => false;
        protected override bool ApplySafeAreaTop => false;
        protected override bool IsFixedContent => false;

        private VisualElement _cardsContainer;
        private readonly System.Collections.Generic.List<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle> _bannerHandles =
            new System.Collections.Generic.List<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>();

        public KnowledgeScreen()
        {
            InitializeComponent(App.current.services
                .GetRequiredService<ITemplateService>()
                .Get(TemplateAddresses.KnowledgeScreen));

            CacheUIElements();
        }

        private void CacheUIElements()
        {
            _cardsContainer = contentContainer.Q<VisualElement>("cards-container");
        }

        protected override void OnViewModelBound()
        {
            base.OnViewModelBound();
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            }
            RebuildCards();
        }

        protected override void OnViewModelUnbinding()
        {
            ReleaseBannerHandles();
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            }
            base.OnViewModelUnbinding();
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_viewModel.Sections))
            {
                RebuildCards();
            }
        }

        private void ReleaseBannerHandles()
        {
            foreach (var handle in _bannerHandles)
            {
                if (handle.IsValid())
                {
                    UnityEngine.AddressableAssets.Addressables.Release(handle);
                }
            }
            _bannerHandles.Clear();
        }

        private void RebuildCards()
        {
            ReleaseBannerHandles();
            if (_cardsContainer == null) return;
            _cardsContainer.Clear();

            var sections = _viewModel?.Sections;
            if (sections == null || sections.Count == 0) return;

            foreach (var section in sections)
            {
                if (section == null) continue;

                var card = new VisualElement();
                card.AddToClassList("fm-knowledge-card");

                // Header Image
                var img = new Image();
                img.AddToClassList("fm-knowledge-card__image");

                if (!string.IsNullOrEmpty(section.BannerAddress))
                {
                    var handle = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<Sprite>(section.BannerAddress);
                    _bannerHandles.Add(handle);
                    handle.Completed += op =>
                    {
                        if (op.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded && op.Result != null)
                        {
                            img.sprite = op.Result;
                            img.scaleMode = ScaleMode.ScaleToFit;
                            if (op.Result.rect.height > 0)
                            {
                                img.style.aspectRatio = op.Result.rect.width / op.Result.rect.height;
                            }
                        }
                    };
                }
                card.Add(img);

                // Footer Row
                var footer = new VisualElement();
                footer.AddToClassList("fm-knowledge-card__footer");

                var title = new Unity.AppUI.UI.Text();
                title.AddToClassList("fm-knowledge-card__title");
                string localizedTitle = null;
                if (!string.IsNullOrEmpty(section.TitleKey))
                {
                    try
                    {
                        localizedTitle = LocalizationSettings.StringDatabase?.GetLocalizedString("UI", section.TitleKey);
                    }
                    catch
                    {
                        // Ignore in tests / non-localized contexts
                    }
                }
                title.text = localizedTitle ?? section.Title ?? section.Id;
                title.size = TextSize.L;
                footer.Add(title);

                var arrow = new Icon();
                arrow.AddToClassList("fm-knowledge-card__arrow");
                arrow.iconName = "chevron-right";
                footer.Add(arrow);

                card.Add(footer);

                // Full clickable overlay button (triggers FoodmissionApp.OnGlobalClick for audio and haptics)
                var capturedSection = section;
                var openBtn = new Unity.AppUI.UI.Button();
                openBtn.AddToClassList("fm-full-button");
                openBtn.quiet = true;
                openBtn.clicked += () => _viewModel?.OpenSection(capturedSection);
                card.Add(openBtn);

                _cardsContainer.Add(card);
            }
        }
    }
}
