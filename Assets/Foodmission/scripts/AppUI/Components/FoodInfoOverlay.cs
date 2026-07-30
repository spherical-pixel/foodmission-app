using System;
using System.ComponentModel;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Localization.Settings;

using Unity.AppUI.MVVM;
using Unity.AppUI.UI;

using eu.foodmission.platform.Components;

namespace eu.foodmission.platform
{
    /// <summary>
    /// Shows food information as a fullscreen overlay without affecting the navigation back-stack.
    /// The underlying screen (Pantry, ShoppingList, MealLog) stays alive so its state is preserved.
    /// Clones the FoodInfoScreen.uxml template and manages the ViewModel lifecycle manually.
    /// Call <see cref="Show"/> to open and <see cref="Dismiss"/> to close.
    /// </summary>
    public static class FoodInfoOverlay
    {
        private static VisualElement _overlay;
        private static FoodInfoViewModel _viewModel;
        private static Unity.AppUI.UI.Modal _modal;
        private static Action _onActionCompleted;

        // UI references (must match the element IDs in FoodInfoScreen.uxml)
        private static VisualElement _foodImageContainer;
        private static UnityEngine.UIElements.Image _foodImage;
        private static Text _foodEmoji;
        private static Heading _foodName;
        private static Text _foodSubtitle;
        private static VisualElement _badgesRow;
        private static Text _nutriscoreBadge;
        private static Text _novaBadge;
        private static Text _ecoBadge;
        private static VisualElement _trafficLights;
        private static VisualElement _macroCards;
        private static Text _macroHeader;
        private static VisualElement _ingredientsSection;
        private static Text _ingredientsBody;
        private static VisualElement _allergensSection;
        private static Text _allergensBody;
        private static VisualElement _nutritionDetailSection;
        private static VisualElement _nutritionDetailTable;
        private static VisualElement _metaSection;
        private static VisualElement _metaBody;
        private static VisualElement _actionContainer;
        private static FMButton _actionButton;
        private static VisualElement _multiActionContainer;
        private static FMButton _btnAddToShoppingList;
        private static FMButton _btnAddToPantry;
        private static FMButton _btnAddToMealLog;
        private static VisualElement _descriptionSection;
        private static Text _descriptionBody;

        private static string _overlayFoodId;
        private static string _overlayFoodData;

        private static string _lastNutriScoreClass = "";
        private static string _lastNovaClass = "";
        private static string _lastEcoClass = "";

        /// <summary>
        /// Shows the FoodInfo overlay.
        /// </summary>
        /// <param name="anchor">Any VisualElement in the current panel — used to reach the root.</param>
        /// <param name="foodType">Product or Generic.</param>
        /// <param name="foodId">Food item ID.</param>
        /// <param name="entryContext">Context used by the action button ("pantry", "shoppingList", "mealLog", "none").</param>
        /// <param name="foodData">Optional pre-serialized JSON food data to avoid an extra API call.</param>
        /// <param name="onActionCompleted">Optional callback invoked after the action button is tapped and the overlay closes.
        /// Use this to call CheckPendingFoodInfoAddRequest() on the underlying screen's ViewModel.</param>
        public static void Show(
            VisualElement anchor,
            FoodInfoType foodType,
            string foodId,
            string entryContext,
            string foodData = null,
            Action onActionCompleted = null)
        {
            Dismiss();

            var root = anchor?.panel?.visualTree;
            if (root == null)
            {
                Debug.LogError("[FoodInfoOverlay] Cannot find panel root to attach overlay.");
                return;
            }

            // Find the main AppUI Panel. This is the container that has all the CSS variables
            // and theming applied. Attaching to it as the last child guarantees we render on top
            // of the navigation stack AND get all correct font sizes and colors.
            VisualElement container = root.Q<Unity.AppUI.UI.Panel>() ?? root;

            // Resolve VM and services from DI
            _viewModel = App.current.services.GetRequiredService<FoodInfoViewModel>();
            _onActionCompleted = onActionCompleted;
            var templateService = App.current.services.GetRequiredService<ITemplateService>();
            var template = templateService.Get(TemplateAddresses.FoodInfo);

            if (template == null)
            {
                Debug.LogError("[FoodInfoOverlay] FoodInfo UXML template not found.");
                _viewModel.Dispose();
                _viewModel = null;
                return;
            }

            // ── Build overlay container ───────────────────────────────────
            _overlay = new VisualElement();
            _overlay.name = "food-info-overlay";
            _overlay.style.position = Position.Absolute;
            _overlay.style.left = 0;
            _overlay.style.right = 0;
            _overlay.style.top = 0;
            _overlay.style.bottom = 0;
            _overlay.pickingMode = PickingMode.Position;
            _overlay.AddToClassList("fm-food-info-overlay");

            var themeService = App.current.services.GetRequiredService<IThemeService>();

            // ── AppBar row ────────────────────────────────────────────────
            var appBar = new VisualElement();
            appBar.AddToClassList("fm-food-info-overlay__appbar");

            // Apply safe area top padding to the appbar (status bar / notch)
            themeService?.ApplySafeAreaPadding(appBar, true, false, false, false);

            var backBtn = new IconButton { icon = "arrow-left", quiet = true };
            backBtn.AddToClassList("fm-food-info-overlay__back-btn");
            backBtn.clicked += Dismiss;
            appBar.Add(backBtn);

            var titleLabel = new Heading();
            titleLabel.name = "overlay-title";
            titleLabel.size = HeadingSize.M;
            titleLabel.AddToClassList("fm-food-info-overlay__title");
            appBar.Add(titleLabel);

            _overlay.Add(appBar);

            // ── Scrollable content (cloned from UXML) ─────────────────────
            var scrollView = new ScrollView(ScrollViewMode.Vertical);
            scrollView.style.flexGrow = 1;
            scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            scrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            // Make scrollview internals transparent so overlay bg shows through
            scrollView.style.backgroundColor = new StyleColor(UnityEngine.Color.clear);
            scrollView.contentContainer.style.backgroundColor = new StyleColor(UnityEngine.Color.clear);
            _overlay.Add(scrollView);

            var content = new VisualElement();
            content.style.flexGrow = 1;
            template.CloneTree(content);
            scrollView.Add(content);

            // ── Cache references ──────────────────────────────────────────
            _foodImageContainer = content.Q<VisualElement>("food-image-container");
            _foodImage = content.Q<UnityEngine.UIElements.Image>("food-image");
            _foodEmoji = content.Q<Text>("food-emoji");
            _foodName = content.Q<Heading>("food-name");
            _foodSubtitle = content.Q<Text>("food-subtitle");
            _badgesRow = content.Q<VisualElement>("badges-row");
            _nutriscoreBadge = content.Q<Text>("nutriscore-badge");
            _novaBadge = content.Q<Text>("nova-badge");
            _ecoBadge = content.Q<Text>("eco-badge");
            _trafficLights = content.Q<VisualElement>("traffic-lights");
            _macroCards = content.Q<VisualElement>("macro-cards");
            _macroHeader = content.Q<Text>("macro-header");
            _ingredientsSection = content.Q<VisualElement>("ingredients-section");
            _ingredientsBody = content.Q<Text>("ingredients-body");
            _allergensSection = content.Q<VisualElement>("allergens-section");
            _allergensBody = content.Q<Text>("allergens-body");
            _nutritionDetailSection = content.Q<VisualElement>("nutrition-detail-section");
            _nutritionDetailTable = content.Q<VisualElement>("nutrition-detail-table");
            _metaSection = content.Q<VisualElement>("meta-section");
            _metaBody = content.Q<VisualElement>("meta-body");
            _actionContainer = content.Q<VisualElement>("action-container");
            _actionButton = content.Q<FMButton>("action-button");
            _multiActionContainer = content.Q<VisualElement>("multi-action-container");
            _btnAddToShoppingList = content.Q<FMButton>("btn-add-to-shopping-list");
            _btnAddToPantry = content.Q<FMButton>("btn-add-to-pantry");
            _btnAddToMealLog = content.Q<FMButton>("btn-add-to-meal-log");
            _descriptionSection = content.Q<VisualElement>("description-section");
            _descriptionBody = content.Q<Text>("description-body");

            _overlayFoodId = foodId;
            _overlayFoodData = foodData;

            // ── Wire VM events ────────────────────────────────────────────
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;

            if (_actionButton != null)
            {
                _actionButton.clicked += OnActionClicked;
            }

            if (_btnAddToShoppingList != null) _btnAddToShoppingList.clicked += OnAddToShoppingListClicked;
            if (_btnAddToPantry != null) _btnAddToPantry.clicked += OnAddToPantryClicked;
            if (_btnAddToMealLog != null) _btnAddToMealLog.clicked += OnAddToMealLogClicked;

            // ── Add to Panel ──────────────────────────────────────────────
            container.Add(_overlay);

            if (_macroHeader != null)
            {
                _macroHeader.text = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "NUTR_VALUES_PER_100G");
            }

            // ── Initial UI state ──────────────────────────────────────────
            UpdateAllSections();

            // ── Load data ─────────────────────────────────────────────────
            _ = _viewModel.LoadAsync(foodType, foodId, entryContext, foodData).ContinueWith(t =>
            {
                if (t.IsFaulted)
                    Debug.LogError($"[FoodInfoOverlay] LoadAsync failed: {t.Exception}");
            }, System.Threading.Tasks.TaskContinuationOptions.OnlyOnFaulted);
        }

        /// <summary>
        /// Dismisses and cleans up the overlay.
        /// </summary>
        public static void Dismiss()
        {
            _onActionCompleted = null;
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
                _viewModel.Dispose();
                _viewModel = null;
            }

            if (_actionButton != null)
            {
                _actionButton.clicked -= OnActionClicked;
                _actionButton = null;
            }

            if (_btnAddToShoppingList != null)
            {
                _btnAddToShoppingList.clicked -= OnAddToShoppingListClicked;
                _btnAddToShoppingList = null;
            }
            if (_btnAddToPantry != null)
            {
                _btnAddToPantry.clicked -= OnAddToPantryClicked;
                _btnAddToPantry = null;
            }
            if (_btnAddToMealLog != null)
            {
                _btnAddToMealLog.clicked -= OnAddToMealLogClicked;
                _btnAddToMealLog = null;
            }
            _multiActionContainer = null;
            _actionContainer = null;
            _overlayFoodId = null;
            _overlayFoodData = null;

            if (_overlay != null && _overlay.parent != null)
            {
                _overlay.RemoveFromHierarchy();
            }
            _overlay = null;

            _foodImageContainer = null;
            _foodImage = null; _foodEmoji = null; _foodName = null; _foodSubtitle = null;
            _badgesRow = null; _nutriscoreBadge = null; _novaBadge = null; _ecoBadge = null;
            _trafficLights = null; _macroCards = null; _macroHeader = null;
            _ingredientsSection = null; _ingredientsBody = null;
            _allergensSection = null; _allergensBody = null;
            _nutritionDetailSection = null; _nutritionDetailTable = null;
            _metaSection = null; _metaBody = null;
            _descriptionSection = null; _descriptionBody = null;
            _lastNutriScoreClass = ""; _lastNovaClass = ""; _lastEcoClass = "";
        }

        // ── VM event handler ──────────────────────────────────────────────

        private static void OnAddToShoppingListClicked() => OnMultiActionClicked("shoppingList");
        private static void OnAddToPantryClicked() => OnMultiActionClicked("pantry");
        private static void OnAddToMealLogClicked() => OnMultiActionClicked("mealLog");

        private static void OnMultiActionClicked(string targetContext)
        {
            var storeService = App.current.services.GetRequiredService<IStoreService>();
            storeService?.store.Dispatch(AppActions.foodInfoAddRequested.Invoke(new AddToContextRequestedAction
            {
                FoodType = _viewModel != null ? _viewModel.FoodType : FoodInfoType.Product,
                FoodId = _overlayFoodId,
                EntryContext = targetContext,
                FoodData = _overlayFoodData
            }));
            var callback = _onActionCompleted;
            Dismiss();
            callback?.Invoke();
        }

        private static void OnActionClicked()
        {
            _viewModel?.OnActionButtonClicked();
            // Capture callback before Dismiss() clears _onActionCompleted.
            var callback = _onActionCompleted;
            Dismiss();
            // Notify the calling screen to process the dispatched Redux action.
            callback?.Invoke();
        }

        private static void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(FoodInfoViewModel.FoodName):
                    if (_foodName != null) _foodName.text = _viewModel.FoodName;
                    // Also update overlay appbar title
                    var overlayTitle = _overlay?.Q<Heading>("overlay-title");
                    if (overlayTitle != null) overlayTitle.text = _viewModel.FoodName;
                    break;
                case nameof(FoodInfoViewModel.FoodSubtitle):
                    if (_foodSubtitle != null) _foodSubtitle.text = _viewModel.FoodSubtitle;
                    break;
                case nameof(FoodInfoViewModel.ImageUrl):
                    UpdateSectionVisibility();
                    _ = LoadImageAsync(_viewModel.ImageUrl);
                    break;
                case nameof(FoodInfoViewModel.Emoji):
                    if (_foodEmoji != null) _foodEmoji.text = _viewModel.Emoji;
                    break;
                case nameof(FoodInfoViewModel.NutritionGrade):
                    UpdateNutriScoreBadge();
                    break;
                case nameof(FoodInfoViewModel.NovaGroup):
                    UpdateNovaBadge();
                    break;
                case nameof(FoodInfoViewModel.EcoScoreGrade):
                    UpdateEcoBadge();
                    break;
                case nameof(FoodInfoViewModel.TrafficLights):
                    UpdateTrafficLights();
                    break;
                case nameof(FoodInfoViewModel.MacroCards):
                    UpdateMacroCards();
                    break;
                case nameof(FoodInfoViewModel.NutritionDetail):
                    UpdateNutritionDetail();
                    break;
                case nameof(FoodInfoViewModel.Ingredients):
                    UpdateIngredients();
                    break;
                case nameof(FoodInfoViewModel.Allergens):
                    UpdateAllergens();
                    break;
                case nameof(FoodInfoViewModel.MetaRows):
                    UpdateMetaRows();
                    break;
                case nameof(FoodInfoViewModel.FoodType):
                    UpdateSectionVisibility();
                    break;
                case nameof(FoodInfoViewModel.IsLoading):
                    UpdateLoadingState();
                    break;
                case nameof(FoodInfoViewModel.ErrorDetail):
                    UpdateApiErrorState();
                    break;
                case nameof(FoodInfoViewModel.ActionButtonText):
                case nameof(FoodInfoViewModel.ShowActionButton):
                case nameof(FoodInfoViewModel.ShowMultipleActions):
                    UpdateActionButtonsVisibility();
                    break;
                case nameof(FoodInfoViewModel.Description):
                    UpdateDescription();
                    break;
                case nameof(FoodInfoViewModel.Traces):
                case nameof(FoodInfoViewModel.Categories):
                case nameof(FoodInfoViewModel.Stores):
                case nameof(FoodInfoViewModel.DietaryFlags):
                    UpdateMetaRows();
                    break;
            }
        }

        // ── Update helpers (must stay in sync with FoodInfoScreen.uxml structure) ──

        private static void UpdateAllSections()
        {
            if (_viewModel == null) return;
            UpdateSectionHeaders();
            if (_foodName != null) _foodName.text = _viewModel.FoodName;
            if (_foodSubtitle != null) _foodSubtitle.text = _viewModel.FoodSubtitle;
            if (_foodEmoji != null) _foodEmoji.text = _viewModel.Emoji;
            UpdateActionButtonsVisibility();
            UpdateNutriScoreBadge();
            UpdateNovaBadge();
            UpdateEcoBadge();
            UpdateTrafficLights();
            UpdateMacroCards();
            UpdateNutritionDetail();
            UpdateIngredients();
            UpdateDescription();
            UpdateAllergens();
            UpdateMetaRows();
            UpdateSectionVisibility();
            UpdateLoadingState();
            _ = LoadImageAsync(_viewModel.ImageUrl);
        }

        private static void UpdateSectionHeaders()
        {
            SetSectionTitle(_ingredientsSection, "SECTION_INGREDIENTS", "Ingredients");
            SetSectionTitle(_allergensSection, "SECTION_ALLERGENS", "Allergens");
            SetSectionTitle(_descriptionSection, "DESCRIPTION", "Description");
            SetSectionTitle(_nutritionDetailSection, "SECTION_NUTRITION_DETAIL", "Full nutrition");
            SetSectionTitle(_metaSection, "SECTION_META", "Product details");
        }

        private static void UpdateActionButtonsVisibility()
        {
            if (_viewModel == null) return;
            if (_actionButton != null)
            {
                _actionButton.title = _viewModel.ActionButtonText;
                _actionButton.EnableInClassList("hidden", !_viewModel.ShowActionButton);
            }
            if (_multiActionContainer != null)
            {
                _multiActionContainer.EnableInClassList("hidden", !_viewModel.ShowMultipleActions);
            }
            if (_actionContainer != null)
            {
                bool showContainer = _viewModel.ShowActionButton || _viewModel.ShowMultipleActions;
                _actionContainer.EnableInClassList("hidden", !showContainer);
            }
        }

        private static void SetSectionTitle(VisualElement section, string locKey, string fallback)
        {
            if (section == null) return;
            string loc = LocalizationSettings.StringDatabase.GetLocalizedString("UI", locKey);
            string title = (string.IsNullOrEmpty(loc) || loc == locKey || loc.StartsWith("No translation found")) ? fallback : loc;

            if (section is AccordionItem accordionItem)
            {
                accordionItem.title = title;
            }
            else
            {
                var heading = section.Q<Heading>();
                if (heading != null) heading.text = title;
            }
        }

        private static void UpdateSectionVisibility()
        {
            if (_viewModel == null) return;
            bool isProduct = _viewModel.FoodType == FoodInfoType.Product;

            bool showImg = isProduct && !string.IsNullOrEmpty(_viewModel.ImageUrl);
            if (_foodImageContainer != null)
                _foodImageContainer.EnableInClassList("fm-fi-section--hidden", !showImg);
            if (_foodImage != null)
            {
                _foodImage.EnableInClassList("hidden", !showImg);
            }
            if (_foodEmoji != null)
            {
                _foodEmoji.EnableInClassList("fm-fi-section--hidden",
                    !(!isProduct && !string.IsNullOrEmpty(_viewModel.Emoji)));
            }
            if (_badgesRow != null)
                _badgesRow.EnableInClassList("fm-fi-section--hidden", !isProduct);

            // Ingredients, allergens, nutrition-detail and meta sections
            // are self-managed by their respective Update*() methods.
        }

        private static void UpdateNutriScoreBadge()
        {
            if (_nutriscoreBadge == null || _viewModel == null) return;
            var pill = _nutriscoreBadge.parent;
            _nutriscoreBadge.RemoveFromClassList(_lastNutriScoreClass);
            string grade = _viewModel.NutritionGrade?.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(grade) || grade == "unknown" || grade == "not-applicable" || grade.Length > 2)
            {
                if (pill != null) pill.style.display = DisplayStyle.Flex;
                _nutriscoreBadge.style.display = DisplayStyle.Flex;
                _nutriscoreBadge.text = "—";
                _lastNutriScoreClass = "fm-fi-score-badge--unknown";
                _nutriscoreBadge.AddToClassList(_lastNutriScoreClass);
                return;
            }
            if (pill != null) pill.style.display = DisplayStyle.Flex;
            _nutriscoreBadge.style.display = DisplayStyle.Flex;
            _nutriscoreBadge.text = grade.ToUpper();
            _lastNutriScoreClass = $"fm-fi-score-badge--{grade}";
            _nutriscoreBadge.AddToClassList(_lastNutriScoreClass);
        }

        private static void UpdateNovaBadge()
        {
            if (_novaBadge == null || _viewModel == null) return;
            var pill = _novaBadge.parent;
            _novaBadge.RemoveFromClassList(_lastNovaClass);
            if (_viewModel.NovaGroup < 1 || _viewModel.NovaGroup > 4)
            {
                if (pill != null) pill.style.display = DisplayStyle.Flex;
                _novaBadge.style.display = DisplayStyle.Flex;
                _novaBadge.text = "—";
                _lastNovaClass = "fm-fi-nova-badge--unknown";
                _novaBadge.AddToClassList(_lastNovaClass);
                return;
            }
            if (pill != null) pill.style.display = DisplayStyle.Flex;
            _novaBadge.style.display = DisplayStyle.Flex;
            _novaBadge.text = $"{_viewModel.NovaGroup}";
            _lastNovaClass = $"fm-fi-nova-badge--{_viewModel.NovaGroup}";
            _novaBadge.AddToClassList(_lastNovaClass);
        }

        private static void UpdateEcoBadge()
        {
            if (_ecoBadge == null || _viewModel == null) return;
            var pill = _ecoBadge.parent;
            _ecoBadge.RemoveFromClassList(_lastEcoClass);
            string grade = _viewModel.EcoScoreGrade?.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(grade) || grade == "unknown" || grade == "not-applicable" || grade.Length > 2)
            {
                if (pill != null) pill.style.display = DisplayStyle.Flex;
                _ecoBadge.style.display = DisplayStyle.Flex;
                _ecoBadge.text = "—";
                _lastEcoClass = "fm-fi-eco-badge--unknown";
                _ecoBadge.AddToClassList(_lastEcoClass);
                return;
            }
            if (pill != null) pill.style.display = DisplayStyle.Flex;
            _ecoBadge.style.display = DisplayStyle.Flex;
            _ecoBadge.text = grade.ToUpper();
            _lastEcoClass = $"fm-fi-eco-badge--{grade}";
            _ecoBadge.AddToClassList(_lastEcoClass);
        }

        private static void UpdateTrafficLights()
        {
            if (_trafficLights == null || _viewModel == null) return;
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

        private static void UpdateMacroCards()
        {
            if (_macroCards == null || _viewModel?.MacroCards == null) return;
            _macroCards.Clear();
            foreach (var card in _viewModel.MacroCards)
            {
                var macroCard = new NutritionMacroCard();
                macroCard.SetData(card.Label, card.Value, card.Unit);
                _macroCards.Add(macroCard);
            }
        }

        private static void UpdateNutritionDetail()
        {
            if (_nutritionDetailTable == null || _viewModel == null) return;
            _nutritionDetailTable.Clear();
            if (_viewModel.NutritionDetail == null || _viewModel.NutritionDetail.Count == 0)
            {
                _nutritionDetailSection?.EnableInClassList("fm-fi-section--hidden", true);
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
                        detailRow.AddToClassList("fm-fi-nutrition-row--odd");
                    _nutritionDetailTable.Add(detailRow);
                    globalRowIndex++;
                }
            }

            _nutritionDetailSection?.EnableInClassList("fm-fi-section--hidden", !hasAnyRows);
        }

        private static void UpdateIngredients()
        {
            if (_viewModel == null) return;
            if (_ingredientsBody != null)
                _ingredientsBody.text = _viewModel.Ingredients;
            bool isProduct = _viewModel.FoodType == FoodInfoType.Product;
            bool show = isProduct && !string.IsNullOrEmpty(_viewModel.Ingredients);
            _ingredientsSection?.EnableInClassList("fm-fi-section--hidden", !show);
        }

        private static void UpdateDescription()
        {
            if (_viewModel == null) return;
            if (_descriptionBody != null)
                _descriptionBody.text = _viewModel.Description;
            bool isProduct = _viewModel.FoodType == FoodInfoType.Product;
            bool show = isProduct && !string.IsNullOrEmpty(_viewModel.Description);
            _descriptionSection?.EnableInClassList("fm-fi-section--hidden", !show);
        }

        private static void UpdateAllergens()
        {
            if (_allergensBody == null || _viewModel == null) return;
            _allergensBody.text = _viewModel.Allergens;
            bool show = !string.IsNullOrEmpty(_viewModel.Allergens);
            _allergensSection?.EnableInClassList("fm-fi-section--hidden", !show);
        }

        private static void UpdateMetaRows()
        {
            if (_metaBody == null || _viewModel == null) return;
            _metaBody.Clear();
            if (_viewModel.MetaRows == null || _viewModel.MetaRows.Count == 0)
            {
                _metaSection?.EnableInClassList("fm-fi-section--hidden", true);
                return;
            }
            _metaSection?.EnableInClassList("fm-fi-section--hidden", false);
            int rowIndex = 0;
            foreach (var row in _viewModel.MetaRows)
            {
                var rowContainer = new VisualElement();
                rowContainer.AddToClassList("fm-fi-meta-row");
                if (rowIndex == 0)
                {
                    rowContainer.AddToClassList("fm-fi-meta-row--first");
                }
                if (rowIndex == _viewModel.MetaRows.Count - 1)
                {
                    rowContainer.AddToClassList("fm-fi-meta-row--last");
                }
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

        private static void UpdateLoadingState()
        {
            if (_overlay == null || _viewModel == null) return;
            if (_viewModel.IsLoading)
                FMLoadingOverlay.Show();
            else
                FMLoadingOverlay.Hide();
        }

        private static void UpdateApiErrorState()
        {
            if (_viewModel?.ErrorDetail == null || _overlay == null) return;
            FMDialog.ShowApiError(_overlay,
                LocalizationSettings.StringDatabase.GetLocalizedString("UI", "ERROR_TITLE"),
                _viewModel.ErrorDetail);
            _viewModel.ErrorDetail = null;
        }

        private static async Task LoadImageAsync(string url)
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
                Debug.LogWarning($"[FoodInfoOverlay] LoadImageAsync failed: {ex.Message}");
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
    }
}
