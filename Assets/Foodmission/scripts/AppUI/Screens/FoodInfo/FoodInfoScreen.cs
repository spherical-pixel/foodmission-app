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
        private VisualElement _foodImageContainer;
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

        // Badge class tracking for clean removal
        private string _lastNutriScoreClass = "";
        private string _lastNovaClass = "";
        private string _lastEcoClass = "";

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
            _foodImageContainer = contentContainer.Q<VisualElement>("food-image-container");
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
            string foodData = null;

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
                    else if (arg.name == "foodData")
                        foodData = arg.value?.ToString();
                }
            }

            base.OnEnter(controller, destination, args);

            _ = _viewModel?.LoadAsync(foodType, foodId, entryContext, foodData).ContinueWith(t =>
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
                    UpdateSectionVisibility();
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

        private static string GetLocOrFallback(string key, string fallback)
        {
            string result = LocalizationSettings.StringDatabase.GetLocalizedString("UI", key);
            if (string.IsNullOrEmpty(result) || result == key) return fallback;
            if (result.StartsWith("No translation found")) return fallback;
            return result;
        }

        private void UpdateAllSections()
        {
            UpdateSectionHeaders();
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

            bool showImg = isProduct && !string.IsNullOrEmpty(_viewModel.ImageUrl);
            if (_foodImageContainer != null)
                _foodImageContainer.EnableInClassList("fm-fi-section--hidden", !showImg);
            if (_foodImage != null)
                _foodImage.EnableInClassList("hidden", !showImg);

            _foodEmoji.EnableInClassList("fm-fi-section--hidden",
                !(! isProduct && !string.IsNullOrEmpty(_viewModel.Emoji)));

            _badgesRow.EnableInClassList("fm-fi-section--hidden", !isProduct);

            // Ingredients, allergens, nutrition-detail and meta sections
            // are self-managed by their respective Update*() methods.
        }

        private void UpdateNutriScoreBadge()
        {
            var pill = _nutriscoreBadge?.parent;

            _nutriscoreBadge.RemoveFromClassList(_lastNutriScoreClass);
            string grade = _viewModel.NutritionGrade?.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(grade) || grade == "unknown" || grade == "not-applicable" || grade.Length > 2)
            {
                if (pill != null) { pill.style.display = DisplayStyle.Flex; }
                _nutriscoreBadge.style.display = DisplayStyle.Flex;
                _nutriscoreBadge.text = "—";
                _lastNutriScoreClass = "fm-fi-score-badge--unknown";
                _nutriscoreBadge.AddToClassList(_lastNutriScoreClass);
                return;
            }
            if (pill != null) { pill.style.display = DisplayStyle.Flex; }
            _nutriscoreBadge.style.display = DisplayStyle.Flex;
            _nutriscoreBadge.text = grade.ToUpper();
            _lastNutriScoreClass = $"fm-fi-score-badge--{grade}";
            _nutriscoreBadge.AddToClassList(_lastNutriScoreClass);
        }

        private void UpdateNovaBadge()
        {
            var pill = _novaBadge?.parent;

            _novaBadge.RemoveFromClassList(_lastNovaClass);
            if (_viewModel.NovaGroup < 1 || _viewModel.NovaGroup > 4)
            {
                if (pill != null) { pill.style.display = DisplayStyle.Flex; }
                _novaBadge.style.display = DisplayStyle.Flex;
                _novaBadge.text = "—";
                _lastNovaClass = "fm-fi-nova-badge--unknown";
                _novaBadge.AddToClassList(_lastNovaClass);
                return;
            }
            if (pill != null) { pill.style.display = DisplayStyle.Flex; }
            _novaBadge.style.display = DisplayStyle.Flex;
            _novaBadge.text = $"{_viewModel.NovaGroup}";
            _lastNovaClass = $"fm-fi-nova-badge--{_viewModel.NovaGroup}";
            _novaBadge.AddToClassList(_lastNovaClass);
        }

        private void UpdateEcoBadge()
        {
            var pill = _ecoBadge?.parent;

            _ecoBadge.RemoveFromClassList(_lastEcoClass);
            string grade = _viewModel.EcoScoreGrade?.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(grade) || grade == "unknown" || grade == "not-applicable" || grade.Length > 2)
            {
                if (pill != null) { pill.style.display = DisplayStyle.Flex; }
                _ecoBadge.style.display = DisplayStyle.Flex;
                _ecoBadge.text = "—";
                _lastEcoClass = "fm-fi-eco-badge--unknown";
                _ecoBadge.AddToClassList(_lastEcoClass);
                return;
            }
            if (pill != null) { pill.style.display = DisplayStyle.Flex; }
            _ecoBadge.style.display = DisplayStyle.Flex;
            _ecoBadge.text = grade.ToUpper();
            _lastEcoClass = $"fm-fi-eco-badge--{grade}";
            _ecoBadge.AddToClassList(_lastEcoClass);
        }

        private void UpdateTrafficLights()
        {
            _trafficLights.Clear();

            if (_viewModel.TrafficLights == null || _viewModel.TrafficLights.Count == 0)
            {
                _trafficLights.style.display = DisplayStyle.None;
                return;
            }
            _trafficLights.style.display = DisplayStyle.Flex;

            foreach (var light in _viewModel.TrafficLights)
            {
                var pill = new VisualElement();
                pill.AddToClassList("fm-fi-traffic-dot-container");

                var dot = new VisualElement();
                dot.AddToClassList("fm-fi-traffic-dot");
                dot.AddToClassList($"fm-fi-traffic-dot--{light.Level?.ToLower() ?? "unknown"}");
                pill.Add(dot);

                var label = new Text { size = TextSize.S, text = light.Label };
                label.AddToClassList("fm-fi-traffic-dot__label");
                pill.Add(label);

                _trafficLights.Add(pill);
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
            if (_viewModel.NutritionDetail == null || _viewModel.NutritionDetail.Count == 0)
            {
                _nutritionDetailSection.EnableInClassList("fm-fi-section--hidden", true);
                Debug.Log($"[FoodInfoScreen] UpdateNutritionDetail — hidden (null or empty)");
                return;
            }

            bool hasAnyRows = false;
            int globalRowIndex = 0;

            foreach (var group in _viewModel.NutritionDetail)
            {
                if (group.Rows == null || group.Rows.Count == 0) continue;
                hasAnyRows = true;

                if (!string.IsNullOrEmpty(group.Title))
                {
                    var groupTitle = new Text { size = TextSize.M, text = group.Title };
                    groupTitle.AddToClassList("fm-fi-nutrition-group-title");
                    _nutritionDetailTable.Add(groupTitle);
                }

                foreach (var row in group.Rows)
                {
                    var detailRow = new NutritionDetailRow();
                    detailRow.SetData(row.Label, row.Value, row.Unit);
                    if (globalRowIndex % 2 != 0)
                    {
                        detailRow.AddToClassList("fm-fi-nutrition-row--odd");
                    }
                    _nutritionDetailTable.Add(detailRow);
                    globalRowIndex++;
                }
            }

            _nutritionDetailSection.EnableInClassList("fm-fi-section--hidden", !hasAnyRows);
            Debug.Log($"[FoodInfoScreen] UpdateNutritionDetail — hasAnyRows={hasAnyRows}, groups={_viewModel.NutritionDetail.Count}, totalRows={globalRowIndex}");
        }

        private void UpdateIngredients()
        {
            _ingredientsBody.text = _viewModel.Ingredients;
            bool isProduct = _viewModel.FoodType == FoodInfoType.Product;
            bool show = isProduct && !string.IsNullOrEmpty(_viewModel.Ingredients);
            _ingredientsSection.EnableInClassList("fm-fi-section--hidden", !show);
        }

        private void SetSectionTitle(VisualElement section, string locKey, string fallback)
        {
            if (section == null) return;
            string text = GetLocOrFallback(locKey, fallback);
            if (section is AccordionItem accordionItem)
            {
                accordionItem.title = text;
            }
            else
            {
                var heading = section.Q<Heading>();
                if (heading != null) heading.text = text;
            }
        }

        private void UpdateSectionHeaders()
        {
            SetSectionTitle(_ingredientsSection, "SECTION_INGREDIENTS", "Ingredients");
            SetSectionTitle(_allergensSection, "SECTION_ALLERGENS", "Allergens");
            SetSectionTitle(_nutritionDetailSection, "SECTION_NUTRITION_DETAIL", "Full nutrition");
            SetSectionTitle(_metaSection, "SECTION_META", "Product details");
        }

        private void UpdateAllergens()
        {
            _allergensBody.text = _viewModel.Allergens;
            bool show = !string.IsNullOrEmpty(_viewModel.Allergens);
            _allergensSection.EnableInClassList("fm-fi-section--hidden", !show);
        }

        private void UpdateMetaRows()
        {
            _metaBody.Clear();
            if (_viewModel.MetaRows == null || _viewModel.MetaRows.Count == 0)
            {
                _metaSection.EnableInClassList("fm-fi-section--hidden", true);
                return;
            }
            _metaSection.EnableInClassList("fm-fi-section--hidden", false);

            int rowIndex = 0;
            foreach (var row in _viewModel.MetaRows)
            {
                var rowContainer = new VisualElement();
                rowContainer.AddToClassList("fm-fi-meta-row");
                if (rowIndex % 2 != 0)
                {
                    rowContainer.AddToClassList("fm-fi-meta-row--odd");
                }

                var labelEl = new Text { size = TextSize.M, text = row.Label };
                labelEl.AddToClassList("fm-fi-meta-row__label");

                var valueEl = new Text { size = TextSize.M, text = row.Value };
                valueEl.AddToClassList("fm-fi-meta-row__value");

                rowContainer.Add(labelEl);
                rowContainer.Add(valueEl);
                _metaBody.Add(rowContainer);
                rowIndex++;
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
                if (_foodImageContainer != null)
                {
                    _foodImageContainer.EnableInClassList("fm-fi-section--hidden", true);
                    _foodImageContainer.style.display = DisplayStyle.None;
                }
                if (_foodImage != null)
                {
                    _foodImage.EnableInClassList("hidden", true);
                    _foodImage.style.display = DisplayStyle.None;
                }
                return;
            }

            try
            {
                var imageService = App.current.services.GetRequiredService<IImageService>();
                Texture2D texture = await imageService.LoadImageAsync(url);

                if (texture != null && _foodImage != null)
                {
                    var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
                    _foodImage.sprite = sprite;
                    _foodImage.scaleMode = ScaleMode.StretchToFill;
                    if (texture.width > 0 && texture.height > 0)
                    {
                        _foodImage.style.aspectRatio = (float)texture.width / texture.height;
                        _foodImage.style.height = StyleKeyword.Null;
                    }
                    _foodImage.EnableInClassList("hidden", false);
                    _foodImage.style.display = DisplayStyle.Flex;
                    if (_foodImageContainer != null)
                    {
                        _foodImageContainer.EnableInClassList("fm-fi-section--hidden", false);
                        _foodImageContainer.style.display = DisplayStyle.Flex;
                    }
                }
                else
                {
                    if (_foodImageContainer != null)
                    {
                        _foodImageContainer.EnableInClassList("fm-fi-section--hidden", true);
                        _foodImageContainer.style.display = DisplayStyle.None;
                    }
                    if (_foodImage != null)
                    {
                        _foodImage.EnableInClassList("hidden", true);
                        _foodImage.style.display = DisplayStyle.None;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[{GetType().Name}] LoadImageAsync failed: {ex.Message}");
                if (_foodImageContainer != null)
                {
                    _foodImageContainer.EnableInClassList("fm-fi-section--hidden", true);
                    _foodImageContainer.style.display = DisplayStyle.None;
                }
                if (_foodImage != null)
                {
                    _foodImage.EnableInClassList("hidden", true);
                    _foodImage.style.display = DisplayStyle.None;
                }
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
