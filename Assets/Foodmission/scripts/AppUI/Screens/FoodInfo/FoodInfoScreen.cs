using System;
using System.ComponentModel;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.UIElements;
using UnityEngine.Localization.Settings;

using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation;
using Unity.AppUI.UI;

using eu.foodmission.platform.Components;

using UnityEngine.Scripting;

namespace eu.foodmission.platform
{
    [Preserve]
    class FoodInfoScreen : NavigationScreenBase<FoodInfoViewModel>
    {
        private UnityEngine.UIElements.Image _foodImage;
        private Text _foodEmoji;
        private Heading _foodName;
        private Text _foodSubtitle;
        private VisualElement _badgesRow;
        private Text _nutriscoreBadge;
        private Text _novaBadge;
        private Text _ecoBadge;
        private VisualElement _trafficLights;
        private VisualElement _macroCards;
        private VisualElement _ingredientsSection;
        private Text _ingredientsBody;
        private VisualElement _allergensSection;
        private Text _allergensBody;
        private VisualElement _nutritionDetailSection;
        private VisualElement _nutritionDetailTable;
        private VisualElement _metaSection;
        private VisualElement _metaBody;
        private Unity.AppUI.UI.Button _actionButton;

        private AccessibilityNode _actionButtonNode;
        private AccessibilityNode _foodImageNode;

        public FoodInfoScreen()
        {
            InitializeComponent(App.current.services
                .GetRequiredService<ITemplateService>()
                .Get(TemplateAddresses.FoodInfo));
            CacheUIElements();
        }

        private void CacheUIElements()
        {
            _foodImage = contentContainer.Q<UnityEngine.UIElements.Image>("food-image");
            _foodEmoji = contentContainer.Q<Text>("food-emoji");
            _foodName = contentContainer.Q<Heading>("food-name");
            _foodSubtitle = contentContainer.Q<Text>("food-subtitle");
            _badgesRow = contentContainer.Q<VisualElement>("badges-row");
            _nutriscoreBadge = contentContainer.Q<Text>("nutriscore-badge");
            _novaBadge = contentContainer.Q<Text>("nova-badge");
            _ecoBadge = contentContainer.Q<Text>("eco-badge");
            _trafficLights = contentContainer.Q<VisualElement>("traffic-lights");
            _macroCards = contentContainer.Q<VisualElement>("macro-cards");
            _ingredientsSection = contentContainer.Q<VisualElement>("ingredients-section");
            _ingredientsBody = contentContainer.Q<Text>("ingredients-body");
            _allergensSection = contentContainer.Q<VisualElement>("allergens-section");
            _allergensBody = contentContainer.Q<Text>("allergens-body");
            _nutritionDetailSection = contentContainer.Q<VisualElement>("nutrition-detail-section");
            _nutritionDetailTable = contentContainer.Q<VisualElement>("nutrition-detail-table");
            _metaSection = contentContainer.Q<VisualElement>("meta-section");
            _metaBody = contentContainer.Q<VisualElement>("meta-body");
            _actionButton = contentContainer.Q<Unity.AppUI.UI.Button>("action-button");
        }

        public override void OnEnter(NavController controller, NavDestination destination, Argument[] args)
        {
            FoodInfoType foodType = FoodInfoType.Product;
            string foodId = "";
            string entryContext = "none";

            if (args != null)
            {
                foreach (var arg in args)
                {
                    if (arg.name == "foodType")
                        foodType = arg.value?.ToString() == "generic" ? FoodInfoType.Generic : FoodInfoType.Product;
                    else if (arg.name == "foodId")
                        foodId = arg.value?.ToString() ?? "";
                    else if (arg.name == "entryContext")
                        entryContext = arg.value?.ToString() ?? "none";
                }
            }

            base.OnEnter(controller, destination, args);

            _ = _viewModel?.LoadAsync(foodType, foodId, entryContext).ContinueWith(t =>
            {
                if (t.IsFaulted)
                    Debug.LogError($"[{GetType().Name}] LoadAsync failed: {t.Exception}");
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        protected override void OnViewModelBound()
        {
            base.OnViewModelBound();

            _actionButton.clicked += OnActionButtonClicked;
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;

            UpdateAllSections();
        }

        protected override void OnViewModelUnbinding()
        {
            _actionButton.clicked -= OnActionButtonClicked;
            if (_viewModel != null)
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            base.OnViewModelUnbinding();
        }

        private void OnActionButtonClicked()
        {
            _viewModel?.OnActionButtonClicked();
            _navController?.PopBackStack();
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(_viewModel.FoodName):
                    _foodName.text = _viewModel.FoodName;
                    if (appBar != null)
                        appBar.title = _viewModel.FoodName;
                    break;
                case nameof(_viewModel.FoodSubtitle):
                    _foodSubtitle.text = _viewModel.FoodSubtitle;
                    break;
                case nameof(_viewModel.ImageUrl):
                    LoadImageAsync(_viewModel.ImageUrl);
                    break;
                case nameof(_viewModel.Emoji):
                    _foodEmoji.text = _viewModel.Emoji;
                    break;
                case nameof(_viewModel.NutritionGrade):
                    UpdateNutriScoreBadge();
                    break;
                case nameof(_viewModel.NovaGroup):
                    UpdateNovaBadge();
                    break;
                case nameof(_viewModel.EcoScoreGrade):
                    UpdateEcoBadge();
                    break;
                case nameof(_viewModel.TrafficLights):
                    UpdateTrafficLights();
                    break;
                case nameof(_viewModel.MacroCards):
                    UpdateMacroCards();
                    break;
                case nameof(_viewModel.NutritionDetail):
                    UpdateNutritionDetail();
                    break;
                case nameof(_viewModel.Ingredients):
                    UpdateIngredients();
                    break;
                case nameof(_viewModel.Allergens):
                    UpdateAllergens();
                    break;
                case nameof(_viewModel.MetaRows):
                    UpdateMetaRows();
                    break;
                case nameof(_viewModel.FoodType):
                    UpdateSectionVisibility();
                    break;
                case nameof(_viewModel.IsLoading):
                    UpdateLoadingState();
                    break;
                case nameof(_viewModel.ErrorDetail):
                    UpdateApiErrorState();
                    break;
                case nameof(_viewModel.ActionButtonText):
                    _actionButton.title = _viewModel.ActionButtonText;
                    break;
                case nameof(_viewModel.ShowActionButton):
                    _actionButton.EnableInClassList("hidden", !_viewModel.ShowActionButton);
                    break;
            }
        }

        private void UpdateAllSections()
        {
            _foodName.text = _viewModel.FoodName;
            _foodSubtitle.text = _viewModel.FoodSubtitle;
            _foodEmoji.text = _viewModel.Emoji;
            _actionButton.title = _viewModel.ActionButtonText;
            _actionButton.EnableInClassList("hidden", !_viewModel.ShowActionButton);
            UpdateNutriScoreBadge();
            UpdateNovaBadge();
            UpdateEcoBadge();
            UpdateTrafficLights();
            UpdateMacroCards();
            UpdateNutritionDetail();
            UpdateIngredients();
            UpdateAllergens();
            UpdateMetaRows();
            UpdateSectionVisibility();
            UpdateLoadingState();
            LoadImageAsync(_viewModel.ImageUrl);
        }

        private void UpdateSectionVisibility()
        {
            bool isProduct = _viewModel.FoodType == FoodInfoType.Product;

            _foodImage.style.display = isProduct && !string.IsNullOrEmpty(_viewModel.ImageUrl) ? DisplayStyle.Flex : DisplayStyle.None;
            _foodEmoji.style.display = !isProduct && !string.IsNullOrEmpty(_viewModel.Emoji) ? DisplayStyle.Flex : DisplayStyle.None;
            _badgesRow.style.display = isProduct ? DisplayStyle.Flex : DisplayStyle.None;
            _ingredientsSection.style.display = isProduct && !string.IsNullOrEmpty(_viewModel.Ingredients) ? DisplayStyle.Flex : DisplayStyle.None;
            _nutritionDetailSection.style.display = _viewModel.NutritionDetail != null && _viewModel.NutritionDetail.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            _metaSection.style.display = _viewModel.MetaRows != null && _viewModel.MetaRows.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void UpdateNutriScoreBadge()
        {
            if (string.IsNullOrEmpty(_viewModel.NutritionGrade))
            {
                _nutriscoreBadge.style.display = DisplayStyle.None;
                return;
            }
            _nutriscoreBadge.style.display = DisplayStyle.Flex;
            _nutriscoreBadge.text = _viewModel.NutritionGrade.ToUpper();
            _nutriscoreBadge.AddToClassList($"fm-fi-score-badge--{_viewModel.NutritionGrade.ToLower()}");
        }

        private void UpdateNovaBadge()
        {
            if (_viewModel.NovaGroup < 1 || _viewModel.NovaGroup > 4)
            {
                _novaBadge.style.display = DisplayStyle.None;
                return;
            }
            _novaBadge.style.display = DisplayStyle.Flex;
            _novaBadge.text = $"NOVA {_viewModel.NovaGroup}";
            _novaBadge.AddToClassList($"fm-fi-nova-badge--{_viewModel.NovaGroup}");
        }

        private void UpdateEcoBadge()
        {
            if (string.IsNullOrEmpty(_viewModel.EcoScoreGrade))
            {
                _ecoBadge.style.display = DisplayStyle.None;
                return;
            }
            _ecoBadge.style.display = DisplayStyle.Flex;
            _ecoBadge.text = _viewModel.EcoScoreGrade.ToUpper();
            _ecoBadge.AddToClassList($"fm-fi-eco-badge--{_viewModel.EcoScoreGrade.ToLower()}");
        }

        private void UpdateTrafficLights()
        {
            if (_viewModel.TrafficLights == null || _viewModel.TrafficLights.Count == 0)
            {
                _trafficLights.style.display = DisplayStyle.None;
                return;
            }
            _trafficLights.style.display = DisplayStyle.Flex;
            _trafficLights.Clear();

            foreach (var light in _viewModel.TrafficLights)
            {
                var dotContainer = new VisualElement();
                dotContainer.AddToClassList("fm-fi-traffic-dot-container");
                dotContainer.style.flexDirection = FlexDirection.Column;
                dotContainer.style.alignItems = Align.Center;

                var dot = new VisualElement();
                dot.AddToClassList("fm-fi-traffic-dot");
                dot.AddToClassList($"fm-fi-traffic-dot--{light.Level?.ToLower() ?? "unknown"}");
                dotContainer.Add(dot);

                var label = new Text { size = TextSize.S, text = light.Label };
                label.AddToClassList("fm-fi-traffic-dot__label");
                dotContainer.Add(label);

                _trafficLights.Add(dotContainer);
            }
        }

        private void UpdateMacroCards()
        {
            _macroCards.Clear();
            if (_viewModel.MacroCards == null) return;

            foreach (var card in _viewModel.MacroCards)
            {
                var macroCard = new NutritionMacroCard();
                macroCard.SetData(card.Label, card.Value, card.Unit);
                _macroCards.Add(macroCard);
            }
        }

        private void UpdateNutritionDetail()
        {
            _nutritionDetailTable.Clear();
            if (_viewModel.NutritionDetail == null) return;

            foreach (var group in _viewModel.NutritionDetail)
            {
                if (group.Rows == null || group.Rows.Count == 0) continue;

                var groupTitle = new Text { size = TextSize.S, text = group.Title };
                groupTitle.AddToClassList("fm-fi-nutrition-group-title");
                _nutritionDetailTable.Add(groupTitle);

                foreach (var row in group.Rows)
                {
                    var detailRow = new NutritionDetailRow();
                    detailRow.SetData(row.Label, row.Value, row.Unit);
                    _nutritionDetailTable.Add(detailRow);
                }
            }
        }

        private void UpdateIngredients()
        {
            _ingredientsBody.text = _viewModel.Ingredients;
        }

        private void UpdateAllergens()
        {
            _allergensBody.text = _viewModel.Allergens;
            _allergensSection.style.display = !string.IsNullOrEmpty(_viewModel.Allergens) ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void UpdateMetaRows()
        {
            _metaBody.Clear();
            if (_viewModel.MetaRows == null || _viewModel.MetaRows.Count == 0)
            {
                _metaSection.style.display = DisplayStyle.None;
                return;
            }
            _metaSection.style.display = DisplayStyle.Flex;

            foreach (var row in _viewModel.MetaRows)
            {
                var rowContainer = new VisualElement();
                rowContainer.style.flexDirection = FlexDirection.Row;
                rowContainer.style.justifyContent = Justify.SpaceBetween;
                rowContainer.style.paddingTop = 4;
                rowContainer.style.paddingBottom = 4;
                rowContainer.style.borderBottomWidth = 1;
                rowContainer.style.borderBottomColor = new StyleColor(new Color(0.8f, 0.8f, 0.8f, 0.3f));

                var label = new Text { size = TextSize.S, text = row.Label };
                label.style.color = new StyleColor(new Color(0.5f, 0.5f, 0.5f));

                var value = new Text { size = TextSize.S, text = row.Value };
                value.style.color = new StyleColor(Color.white);

                rowContainer.Add(label);
                rowContainer.Add(value);
                _metaBody.Add(rowContainer);
            }
        }

        private void UpdateLoadingState()
        {
            if (_viewModel.IsLoading)
                FMLoadingOverlay.Show(contentContainer);
            else
                FMLoadingOverlay.Hide(contentContainer);
        }

        private void UpdateApiErrorState()
        {
            if (_viewModel.ErrorDetail != null)
            {
                FMDialog.ShowApiError(this, LocalizationSettings.StringDatabase.GetLocalizedString("UI", "ERROR_TITLE"), _viewModel.ErrorDetail);
                _viewModel.ErrorDetail = null;
            }
        }

        private async void LoadImageAsync(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                _foodImage.style.display = DisplayStyle.None;
                return;
            }

            try
            {
                var imageService = App.current.services.GetRequiredService<IImageService>();
                Texture2D texture = await imageService.LoadImageAsync(url);

                if (texture != null)
                {
                    var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
                    _foodImage.sprite = sprite;
                    _foodImage.style.display = DisplayStyle.Flex;
                }
                else
                {
                    _foodImage.style.display = DisplayStyle.None;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[{GetType().Name}] LoadImageAsync failed: {ex.Message}");
                _foodImage.style.display = DisplayStyle.None;
            }
        }

        protected override void SetupAccessibilityNodes()
        {
            base.SetupAccessibilityNodes();
            if (_accessibilityHierarchy == null) return;

            _actionButtonNode = CreateButtonNode(_accessibilityHierarchy, _actionButton, "Add to context");

            if (_foodImage != null)
            {
                _foodImageNode = _accessibilityHierarchy.AddNode("Food image");
                _foodImageNode.role = AccessibilityRole.Image;
                _foodImageNode.frameGetter = MakeElementFrameGetter(_foodImage);
            }
        }

        protected override void TeardownAccessibilityNodes()
        {
            _actionButtonNode = null;
            _foodImageNode = null;
            base.TeardownAccessibilityNodes();
        }

        private AccessibilityNode CreateButtonNode(AccessibilityHierarchy hierarchy, VisualElement button, string label)
        {
            if (button == null) return null;
            var node = hierarchy.AddNode(label);
            node.role = AccessibilityRole.Button;
            if (!button.enabledSelf) node.state = AccessibilityState.Disabled;
            node.frameGetter = () =>
            {
                if (button.panel == null) return Rect.zero;
                var r = button.worldBound;
                var s = button.panel.scaledPixelsPerPoint;
                return new Rect(r.position * s, r.size * s);
            };
            node.invoked += () =>
            {
                using var evt = NavigationSubmitEvent.GetPooled();
                evt.target = button;
                button.SendEvent(evt);
                return true;
            };
            return node;
        }

        private static Func<Rect> MakeElementFrameGetter(VisualElement element)
        {
            return () =>
            {
                if (element == null || element.panel == null) return Rect.zero;
                var r = element.worldBound;
                var s = element.panel.scaledPixelsPerPoint;
                return new Rect(r.position * s, r.size * s);
            };
        }
    }
}
