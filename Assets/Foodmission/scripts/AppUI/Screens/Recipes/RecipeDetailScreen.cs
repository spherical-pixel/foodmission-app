using System;
using System.ComponentModel;
using System.Collections.Generic;
using Unity.AppUI.UI;
using Unity.AppUI.Navigation;
using UnityEngine.UIElements;
using UnityEngine;
using UnityEngine.Scripting;
using eu.foodmission.platform;
using eu.foodmission.platform.Components;
using Unity.AppUI.MVVM;
using UnityEngine.Localization.Settings;
using System.Linq;

namespace eu.foodmission.platform
{
    [Preserve]
    public class RecipeDetailScreen : NavigationScreenBase<RecipeDetailViewModel>
    {
        private Image _heroImage;
        private VisualElement _heroImageContainer;
        private Text _heroEmoji;
        private Heading _recipeTitle;
        private Text _ratingText;
        private VisualElement _badgesStrip;
        private Text _description;

        private VisualElement _ingredientsSection;
        private VisualElement _ingredientsCard;
        private VisualElement _instructionsSection;
        private VisualElement _instructionsContainer;
        private VisualElement _nutritionSection;
        private VisualElement _nutritionCard;
        private VisualElement _dietarySection;
        private VisualElement _dietaryContainer;
        private VisualElement _allergensSection;
        private VisualElement _allergensContainer;
        private VisualElement _tagsSection;
        private VisualElement _tagsContainer;
        private VisualElement _metaSection;
        private VisualElement _metaCard;

        private Unity.AppUI.UI.Button _btnLog;
        private Unity.AppUI.UI.Button _btnAddToShoppingList;
        private Unity.AppUI.UI.Button _btnEdit;
        private Unity.AppUI.UI.Button _btnDelete;
        private Unity.AppUI.UI.Button _btnWatchVideo;

        protected override bool ApplySafeAreaTop => false;
        protected override bool ApplySafeAreaBottom => false;
        protected override bool IsFixedContent => false;
        public RecipeDetailScreen()
        {
            InitializeComponent(App.current.services
                .GetRequiredService<ITemplateService>()
                .Get(TemplateAddresses.RecipeDetail));
            CacheUIElements();
        }

        private void CacheUIElements()
        {
            _heroImage = contentContainer.Q<Image>("hero-image");
            _heroImageContainer = contentContainer.Q<VisualElement>("hero-image-container");
            _heroEmoji = contentContainer.Q<Text>("hero-emoji");
            _recipeTitle = contentContainer.Q<Heading>("recipe-title");
            _ratingText = contentContainer.Q<Text>("rating-text");
            _badgesStrip = contentContainer.Q<VisualElement>("badges-strip");
            _description = contentContainer.Q<Text>("description");

            _ingredientsSection = contentContainer.Q<VisualElement>("ingredients-section");
            _ingredientsCard = contentContainer.Q<VisualElement>("ingredients-card");
            _instructionsSection = contentContainer.Q<VisualElement>("instructions-section");
            _instructionsContainer = contentContainer.Q<VisualElement>("instructions-container");
            _nutritionSection = contentContainer.Q<VisualElement>("nutrition-section");
            _nutritionCard = contentContainer.Q<VisualElement>("nutrition-card");
            _dietarySection = contentContainer.Q<VisualElement>("dietary-section");
            _dietaryContainer = contentContainer.Q<VisualElement>("dietary-container");
            _allergensSection = contentContainer.Q<VisualElement>("allergens-section");
            _allergensContainer = contentContainer.Q<VisualElement>("allergens-container");
            _tagsSection = contentContainer.Q<VisualElement>("tags-section");
            _tagsContainer = contentContainer.Q<VisualElement>("tags-container");
            _metaSection = contentContainer.Q<VisualElement>("meta-section");
            _metaCard = contentContainer.Q<VisualElement>("meta-card");

            _btnLog = contentContainer.Q<Unity.AppUI.UI.Button>("btn-log");
            _btnAddToShoppingList = contentContainer.Q<Unity.AppUI.UI.Button>("btn-add-to-shopping-list");
            _btnEdit = contentContainer.Q<Unity.AppUI.UI.Button>("btn-edit");
            _btnDelete = contentContainer.Q<Unity.AppUI.UI.Button>("btn-delete");
            _btnWatchVideo = contentContainer.Q<Unity.AppUI.UI.Button>("btn-watch-video");
        }

        public override void OnEnter(NavController controller, NavDestination destination, Argument[] args)
        {
            base.OnEnter(controller, destination, args);
            string recipeId = ExtractArg(args, "recipeId");
            if (!string.IsNullOrEmpty(recipeId))
                _ = _viewModel.LoadAsync(recipeId);
        }

        private static string ExtractArg(Argument[] args, string name)
        {
            if (args == null) return null;
            foreach (var a in args)
                if (a.name == name) return a.value as string;
            return null;
        }

        protected override void OnViewModelBound()
        {
            base.OnViewModelBound();

            if (_btnLog != null)
            {
                _btnLog.clickable.clicked += OnLogClicked;
            }
            if (_btnAddToShoppingList != null)
            {
                _btnAddToShoppingList.clickable.clicked += OnAddToShoppingListClicked;
            }
            if (_btnEdit != null)
            {
                _btnEdit.clickable.clicked += OnEditClicked;
            }

            if (_btnDelete != null)
            {
                _btnDelete.clickable.clicked += OnDeleteClicked;
            }

            if (_btnWatchVideo != null)
            {
                _btnWatchVideo.clickable.clicked += OnWatchVideoClicked;
            }

            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        protected override void OnViewModelUnbinding()
        {
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            }

            if (_btnLog != null)
            {
                _btnLog.clickable.clicked -= OnLogClicked;
            }

            if (_btnAddToShoppingList != null)
            {
                _btnAddToShoppingList.clickable.clicked -= OnAddToShoppingListClicked;
            }

            if (_btnEdit != null)
            {
                _btnEdit.clickable.clicked -= OnEditClicked;
            }

            if (_btnDelete != null)
            {
                _btnDelete.clickable.clicked -= OnDeleteClicked;
            }

            if (_btnWatchVideo != null)
            {
                _btnWatchVideo.clickable.clicked -= OnWatchVideoClicked;
            }

            base.OnViewModelUnbinding();
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(_viewModel.Recipe):
                    RebuildAll();
                    break;
                case nameof(_viewModel.IsLoading):
                    UpdateLoadingState();
                    break;
                case nameof(_viewModel.IsOwner):
                    UpdateActionButtonsVisibility();
                    break;
                case nameof(_viewModel.HasVideo):
                    UpdateVideoButtonVisibility();
                    break;
                case nameof(_viewModel.IsAddingToShoppingList):
                    UpdateAddToShoppingListState();
                    break;
                case nameof(_viewModel.ErrorDetail):
                    UpdateErrorState();
                    break;
            }
        }

        private void RebuildAll()
        {
            var r = _viewModel.Recipe;
            if (r == null)
            {
                Debug.Log("[RecipeDetailScreen] RebuildAll skipped: _viewModel.Recipe is null");
                return;
            }

            Debug.Log($"[RecipeDetailScreen] RebuildAll: title='{r.title}', category='{r.category}', cuisineType='{r.cuisineType}', servings={r.servings}, eco={r.sustainabilityScore}");

            if (_recipeTitle != null) _recipeTitle.text = r.title ?? "";
            if (_ratingText != null)
            {
                var ratingStr = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "RATING") + ":";
                if (r.ratingCount > 0)
                {
                    ratingStr += $" {r.rating:F1}/5 ({r.ratingCount})";
                }
                else
                {
                    ratingStr += LocalizationSettings.StringDatabase.GetLocalizedString("UI", "NO_REVIEWS");
                }
                _ratingText.text = ratingStr;
            }

            if (_description != null)
            {
                if (!string.IsNullOrWhiteSpace(r.description))
                {
                    _description.text = r.description;
                    _description.style.display = DisplayStyle.Flex;
                }
                else
                {
                    _description.style.display = DisplayStyle.None;
                }
            }

            LoadHeroImage(r.imageUrl);
            RebuildBadges(r);
            RebuildIngredients(r);
            RebuildInstructions(r);
            RebuildNutrition(r);
            RebuildDietary(r);
            RebuildAllergens(r);
            RebuildTags(r);
            RebuildMeta(r);
            UpdateVideoButtonVisibility();
        }

        private async void LoadHeroImage(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                if (_heroImageContainer != null)
                {
                    _heroImageContainer.EnableInClassList("fm-rd-section--hidden", true);
                    _heroImageContainer.style.display = DisplayStyle.None;
                }
                if (_heroImage != null)
                {
                    _heroImage.sprite = null;
                    _heroImage.style.display = DisplayStyle.None;
                }
                if (_heroEmoji != null)
                    _heroEmoji.style.display = DisplayStyle.Flex;
                return;
            }

            try
            {
                var imageService = App.current?.services?.GetService(typeof(IImageService)) as IImageService;
                if (imageService == null) return;

                var texture = await imageService.LoadImageAsync(url);
                if (texture != null && _heroImage != null)
                {
                    var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
                    _heroImage.sprite = sprite;
                    _heroImage.scaleMode = ScaleMode.StretchToFill;
                    if (texture.width > 0 && texture.height > 0)
                    {
                        _heroImage.style.aspectRatio = (float)texture.width / texture.height;
                        _heroImage.style.height = StyleKeyword.Null;
                    }
                    _heroImage.style.display = DisplayStyle.Flex;
                    if (_heroImageContainer != null)
                    {
                        _heroImageContainer.EnableInClassList("fm-rd-section--hidden", false);
                        _heroImageContainer.style.display = DisplayStyle.Flex;
                    }
                    if (_heroEmoji != null)
                        _heroEmoji.style.display = DisplayStyle.None;
                }
                else
                {
                    if (_heroImageContainer != null)
                    {
                        _heroImageContainer.EnableInClassList("fm-rd-section--hidden", true);
                        _heroImageContainer.style.display = DisplayStyle.None;
                    }
                    if (_heroImage != null)
                    {
                        _heroImage.sprite = null;
                        _heroImage.style.display = DisplayStyle.None;
                    }
                    if (_heroEmoji != null)
                        _heroEmoji.style.display = DisplayStyle.Flex;
                }
            }
            catch
            {
                if (_heroImageContainer != null)
                {
                    _heroImageContainer.EnableInClassList("fm-rd-section--hidden", true);
                    _heroImageContainer.style.display = DisplayStyle.None;
                }
                if (_heroImage != null)
                {
                    _heroImage.sprite = null;
                    _heroImage.style.display = DisplayStyle.None;
                }
                if (_heroEmoji != null)
                    _heroEmoji.style.display = DisplayStyle.Flex;
            }
        }

        private void RebuildBadges(Recipe r)
        {
            if (_badgesStrip == null) return;
            _badgesStrip.Clear();

            r.prepTime = 60;
            r.cookTime = 100;
            r.difficulty = "hard";
            // r.sustainabilityScore = 5;
            r.servings = 1;
            // r.category = "Pasta";
            // r.cuisineType = "Italian";

            var totalTime = (r.prepTime ?? 0) + (r.cookTime ?? 0);
            if (totalTime > 0)
            {
                AddBadgeToContainer(_badgesStrip, LocalizationSettings.StringDatabase.GetLocalizedString("UI", "TIME_BADGE_MIN", null, FallbackBehavior.UseProjectSettings, totalTime), "fm-r-badge--metric");

            }

            if ((r.servings ?? 0) > 0)
            {
                AddBadgeToContainer(_badgesStrip, LocalizationSettings.StringDatabase.GetLocalizedString("UI", "SERVINGS_BADGE", null, FallbackBehavior.UseProjectSettings, r.servings), "fm-r-badge--metric");
            }

            if (!string.IsNullOrEmpty(r.difficulty))
            {
                var (diffLabel, diffClass) = r.difficulty.ToLowerInvariant() switch
                {
                    "easy" => (LocalizationSettings.StringDatabase.GetLocalizedString("UI", "DIFF_EASY_BADGE"), "fm-r-badge--easy"),
                    "medium" => (LocalizationSettings.StringDatabase.GetLocalizedString("UI", "DIFF_MED_BADGE"), "fm-r-badge--medium"),
                    "hard" => (LocalizationSettings.StringDatabase.GetLocalizedString("UI", "DIFF_HARD_BADGE"), "fm-r-badge--hard"),
                    _ => (r.difficulty, "fm-r-badge--medium")
                };
                AddBadgeToContainer(_badgesStrip, diffLabel, diffClass);
            }



            if (r.sustainabilityScore.HasValue)
            {
                int ecoPct = Mathf.RoundToInt(r.sustainabilityScore.Value * 100);
                AddBadgeToContainer(_badgesStrip, $"🌱 Eco {ecoPct}%", "fm-r-badge--easy");
            }

            if (!string.IsNullOrEmpty(r.category))
            {
                AddBadgeToContainer(_badgesStrip, $"🥗 {r.category}", "fm-r-badge--metric");
            }

            if (!string.IsNullOrEmpty(r.cuisineType))
            {
                AddBadgeToContainer(_badgesStrip, $"🍝 {r.cuisineType}", "fm-r-badge--metric");
            }
        }

        private void RebuildDietary(Recipe r)
        {
            if (_dietarySection == null || _dietaryContainer == null) return;
            _dietaryContainer.Clear();

            if (r?.dietaryLabels == null || r.dietaryLabels.Length == 0)
            {
                _dietarySection.style.display = DisplayStyle.None;
                return;
            }

            foreach (var label in r.dietaryLabels)
            {
                if (string.IsNullOrWhiteSpace(label)) continue;
                AddBadgeToContainer(_dietaryContainer, $"🌱 {label}", "fm-r-badge--dietary");
            }

            _dietarySection.style.display = _dietaryContainer.childCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void RebuildAllergens(Recipe r)
        {
            if (_allergensSection == null || _allergensContainer == null) return;
            _allergensContainer.Clear();

            if (r?.allergens == null || r.allergens.Length == 0)
            {
                _allergensSection.style.display = DisplayStyle.None;
                return;
            }

            foreach (var allergen in r.allergens)
            {
                if (string.IsNullOrWhiteSpace(allergen)) continue;
                AddBadgeToContainer(_allergensContainer, $"⚠️ {allergen}", "fm-r-badge--allergen");
            }

            _allergensSection.style.display = _allergensContainer.childCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void RebuildTags(Recipe r)
        {
            if (_tagsSection == null || _tagsContainer == null) return;
            _tagsContainer.Clear();

            if (r?.tags == null || r.tags.Length == 0)
            {
                _tagsSection.style.display = DisplayStyle.None;
                return;
            }

            foreach (var tag in r.tags)
            {
                if (string.IsNullOrWhiteSpace(tag)) continue;
                var tagText = tag.StartsWith("#") ? tag : $"#{tag}";
                AddBadgeToContainer(_tagsContainer, tagText, "fm-r-badge--tag");
            }

            _tagsSection.style.display = _tagsContainer.childCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void AddBadgeToContainer(VisualElement container, string text, string cssClass)
        {
            var chip = new Text { text = text };
            chip.style.whiteSpace = WhiteSpace.NoWrap;
            chip.AddToClassList("fm-r-badge");
            if (!string.IsNullOrEmpty(cssClass))
            {
                chip.AddToClassList(cssClass);
            }
            container.Add(chip);
        }

        private void RebuildIngredients(Recipe r)
        {
            if (_ingredientsSection == null || _ingredientsCard == null) return;
            _ingredientsCard.Clear();

            if (r?.ingredients == null || r.ingredients.Length == 0)
            {
                _ingredientsSection.style.display = DisplayStyle.None;
                return;
            }

            _ingredientsSection.style.display = DisplayStyle.Flex;
            for (int i = 0; i < r.ingredients.Length; i++)
            {
                var ing = r.ingredients[i];
                var row = new VisualElement();
                row.AddToClassList("fm-rd-detail-row");
                if (i % 2 != 0)
                {
                    row.AddToClassList("fm-rd-detail-row--odd");
                }

                // var indexEl = new Text { text = $"{i + 1}" };
                // indexEl.AddToClassList("fm-rd-ingredient-index");
                // row.Add(indexEl);

                var nameEl = new Text { text = ing.name ?? "" };
                nameEl.AddToClassList("fm-rd-detail-row__label");
                row.Add(nameEl);

                if (!string.IsNullOrEmpty(ing.measure))
                {
                    var measureEl = new Text { text = ing.measure };
                    measureEl.AddToClassList("fm-rd-detail-row__value");
                    row.Add(measureEl);
                }
                _ingredientsCard.Add(row);
            }
        }

        private void RebuildInstructions(Recipe r)
        {
            if (_instructionsSection == null || _instructionsContainer == null) return;
            _instructionsContainer.Clear();

            var text = r?.instructions ?? "";
            if (string.IsNullOrWhiteSpace(text))
            {
                _instructionsSection.style.display = DisplayStyle.None;
                return;
            }

            _instructionsSection.style.display = DisplayStyle.Flex;
            var paragraphs = text.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (paragraphs.Length == 0)
            {
                var single = new Text { text = text };
                single.AddToClassList("fm-rd-instructions");
                _instructionsContainer.Add(single);
                return;
            }

            for (int i = 0; i < paragraphs.Length; i++)
            {
                if (i > 0)
                {
                    var divider = new VisualElement();
                    divider.AddToClassList("fm-rd-instruction-divider");
                    _instructionsContainer.Add(divider);
                }
                var para = new Text { text = paragraphs[i].Trim() };
                para.AddToClassList("fm-rd-instructions");
                _instructionsContainer.Add(para);
            }
        }

        private void RebuildMeta(Recipe r)
        {
            if (_metaSection == null)
            {
                _metaSection = contentContainer.Q<VisualElement>("meta-section");
            }

            if (_metaCard == null && _metaSection != null)
            {
                _metaCard = _metaSection.Q<VisualElement>("meta-card") ?? contentContainer.Q<VisualElement>("meta-card");
            }

            Debug.Log($"[RecipeDetailScreen] RebuildMeta: _metaSection={_metaSection != null}, _metaCard={_metaCard != null}");
            if (_metaSection == null || _metaCard == null) return;
            _metaCard.Clear();

            int rowIndex = 0;

            var totalTime = (r?.prepTime ?? 0) + (r?.cookTime ?? 0);
            if (totalTime > 0)
            {
                AddDetailRow(_metaCard,
                    LocalizationSettings.StringDatabase.GetLocalizedString("UI", "TOTAL_TIME"),
                    LocalizationSettings.StringDatabase.GetLocalizedString("UI", "TIME_ROW_MIN", null, FallbackBehavior.UseProjectSettings, totalTime),
                    ref rowIndex);
            }

            if (r?.prepTime.HasValue == true && r.prepTime.Value > 0)
            {
                AddDetailRow(_metaCard,
                    LocalizationSettings.StringDatabase.GetLocalizedString("UI", "PREPARATION_TIME"),
                    LocalizationSettings.StringDatabase.GetLocalizedString("UI", "TIME_ROW_MIN", null, FallbackBehavior.UseProjectSettings, r.prepTime.Value),
                    ref rowIndex);
            }

            if (r?.cookTime.HasValue == true && r.cookTime.Value > 0)
            {
                AddDetailRow(_metaCard,
                    LocalizationSettings.StringDatabase.GetLocalizedString("UI", "COOK_TIME"),
                    LocalizationSettings.StringDatabase.GetLocalizedString("UI", "TIME_ROW_MIN", null, FallbackBehavior.UseProjectSettings, r.cookTime.Value),
                    ref rowIndex);
            }

            if (r?.servings.HasValue == true && r.servings.Value > 0)
            {
                AddDetailRow(_metaCard,
                    LocalizationSettings.StringDatabase.GetLocalizedString("UI", "SERVINGS"),
                    r.servings.Value.ToString(),
                    ref rowIndex);
            }

            if (!string.IsNullOrEmpty(r?.difficulty))
            {
                AddDetailRow(_metaCard,
                    LocalizationSettings.StringDatabase.GetLocalizedString("UI", "DIFFICULTY"),
                    r.difficulty,
                    ref rowIndex);
            }

            if (!string.IsNullOrEmpty(r?.cuisineType))
            {
                AddDetailRow(_metaCard,
                    LocalizationSettings.StringDatabase.GetLocalizedString("UI", "CUISINE"), // 
                    r.cuisineType,
                    ref rowIndex);
            }

            if (!string.IsNullOrEmpty(r?.category))
            {
                AddDetailRow(_metaCard,
                    LocalizationSettings.StringDatabase.GetLocalizedString("UI", "CATEGORY"),
                    r.category,
                    ref rowIndex);
            }

            if (r?.sustainabilityScore.HasValue == true)
            {
                AddDetailRow(_metaCard,
                    LocalizationSettings.StringDatabase.GetLocalizedString("UI", "ECO_SCORE"),
                    $"{Mathf.RoundToInt(r.sustainabilityScore.Value * 100)}%",
                    ref rowIndex);
            }

            if (r?.price.HasValue == true && r.price.Value > 0)
            {
                AddDetailRow(_metaCard,
                    LocalizationSettings.StringDatabase.GetLocalizedString("UI", "ESTIMATED_PRICE"),
                    $"{r.price.Value:C2}",
                    ref rowIndex);
            }

            var display = _metaCard.childCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            _metaSection.style.display = display;
            Debug.Log($"[RecipeDetailScreen] RebuildMeta completed: {_metaCard.childCount} rows added, _metaSection display set to {display}");
        }

        private void RebuildNutrition(Recipe r)
        {
            if (_nutritionSection == null || _nutritionCard == null) return;
            _nutritionCard.Clear();

            var info = r?.nutritionalInfo;
            if (info == null || (!info.energyKcal.HasValue && !info.protein.HasValue && !info.carbs.HasValue && !info.fat.HasValue && !info.fiber.HasValue))
            {
                _nutritionSection.style.display = DisplayStyle.None;
                return;
            }

            _nutritionSection.style.display = DisplayStyle.Flex;
            int rowIndex = 0;
            if (info.energyKcal.HasValue)
            {
                AddDetailRow(_nutritionCard,
                    LocalizationSettings.StringDatabase.GetLocalizedString("UI", "NUTR_ENERGY_KCAL"),
                    $"{info.energyKcal.Value:F1} kcal",
                    ref rowIndex);
            }

            if (info.protein.HasValue)
            {
                AddDetailRow(_nutritionCard,
                    LocalizationSettings.StringDatabase.GetLocalizedString("UI", "NUTR_PROTEINS"),
                    $"{info.protein.Value:F1}g",
                    ref rowIndex);
            }

            if (info.carbs.HasValue)
            {
                AddDetailRow(_nutritionCard,
                    LocalizationSettings.StringDatabase.GetLocalizedString("UI", "NUTR_CARBOHYDRATES"),
                    $"{info.carbs.Value:F1}g",
                    ref rowIndex);
            }

            if (info.fat.HasValue)
            {
                AddDetailRow(_nutritionCard,
                    LocalizationSettings.StringDatabase.GetLocalizedString("UI", "NUTR_FAT"),
                    $"{info.fat.Value:F1}g",
                    ref rowIndex);
            }


            if (info.fiber.HasValue)
            {
                AddDetailRow(_nutritionCard,
                    LocalizationSettings.StringDatabase.GetLocalizedString("UI", "NUTR_FIBER"),
                    $"{info.fiber.Value:F1}g",
                    ref rowIndex);
            }
        }

        private void AddDetailRow(VisualElement containerCard, string label, string value, ref int rowIndex)
        {
            if (string.IsNullOrEmpty(value)) return;

            var row = new VisualElement();
            row.AddToClassList("fm-rd-detail-row");
            if (rowIndex % 2 != 0)
            {
                row.AddToClassList("fm-rd-detail-row--odd");
            }

            var labelEl = new Text { text = label };
            labelEl.AddToClassList("fm-rd-detail-row__label");
            row.Add(labelEl);

            var valueEl = new Text { text = value };
            valueEl.AddToClassList("fm-rd-detail-row__value");
            row.Add(valueEl);

            containerCard.Add(row);
            rowIndex++;
        }

        private void UpdateLoadingState()
        {
            if (_viewModel.IsLoading)
            {
                FMLoadingOverlay.Show(contentContainer);
            }
            else
            {
                FMLoadingOverlay.Hide(contentContainer);
            }
        }

        private void UpdateActionButtonsVisibility()
        {
            _btnEdit?.EnableInClassList("fm-rd-action--hidden", !_viewModel.IsOwner);
            _btnDelete?.EnableInClassList("fm-rd-action--hidden", !_viewModel.IsOwner);
        }

        private void UpdateAddToShoppingListState()
        {
            _btnAddToShoppingList?.SetEnabled(!_viewModel.IsAddingToShoppingList);
        }

        private void UpdateErrorState()
        {
            if (_viewModel.ErrorDetail != null)
            {
                FMDialog.ShowApiError(this, "RECIPE_ERROR_LOAD", _viewModel.ErrorDetail);
                _viewModel.ErrorDetail = null;
            }
        }

        private void UpdateVideoButtonVisibility()
        {
            if (_btnWatchVideo == null) return;
            bool hasVideo = _viewModel.HasVideo || !string.IsNullOrWhiteSpace(_viewModel.Recipe?.videoUrl);
            Debug.Log($"[RecipeDetailScreen] UpdateVideoButtonVisibility: videoUrl='{_viewModel.Recipe?.videoUrl}', hasVideo={hasVideo}");
            _btnWatchVideo.EnableInClassList("fm-rd-action--hidden", !hasVideo);
            _btnWatchVideo.style.display = hasVideo ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void OnWatchVideoClicked()
        {
            var url = _viewModel.Recipe?.videoUrl;
            if (!string.IsNullOrEmpty(url))
                Application.OpenURL(url);
        }

        private void OnLogClicked()
        {
            var recipe = _viewModel?.Recipe;
            if (recipe == null) return;
            ShowQuickMealLogOptionsDialog(recipe);
        }

        private async void ShowQuickMealLogOptionsDialog(Recipe recipe)
        {
            var content = new VisualElement();
            content.style.paddingTop = 16;
            content.style.paddingBottom = 16;
            content.style.paddingLeft = 16;
            content.style.paddingRight = 16;

            var foodLabel = new Unity.AppUI.UI.Text
            {
                size = TextSize.M,
                text = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "ADD_ITEM_TITLE") + $": {recipe.title}"
            };
            foodLabel.style.marginBottom = 16;
            content.Add(foodLabel);

            var mealTypeHeader = new Unity.AppUI.UI.Text
            {
                size = TextSize.M,
                text = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "TYPE_OF_MEAL") + ":"
            };
            mealTypeHeader.style.marginBottom = 8;
            content.Add(mealTypeHeader);

            var catalogItems = _viewModel != null ? await _viewModel.GetMealTypesAsync() : null;
            var mealTypeNames = new System.Collections.Generic.List<string>();

            if (catalogItems != null && catalogItems.Length > 0)
            {
                foreach (var item in catalogItems)
                {
                    mealTypeNames.Add(item.label ?? item.code);
                }
            }

            var mealTypeDropdown = new Dropdown();
            mealTypeDropdown.bindItem = (item, i) => item.label = mealTypeNames[i];
            mealTypeDropdown.sourceItems = mealTypeNames;
            mealTypeDropdown.SetValueWithoutNotify(new[] { 0 });
            mealTypeDropdown.style.marginBottom = 16;
            content.Add(mealTypeDropdown);

            var sourceHeader = new Unity.AppUI.UI.Text
            {
                size = TextSize.S,
                text = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "ORIGIN") + ":"
            };
            sourceHeader.style.marginBottom = 8;
            content.Add(sourceHeader);

            bool isFromPantry = true;
            var pantryCheckbox = new Unity.AppUI.UI.Checkbox();
            pantryCheckbox.label = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "FROM_PANTRY");
            pantryCheckbox.value = CheckboxState.Checked;
            pantryCheckbox.RegisterValueChangedCallback(evt =>
            {
                isFromPantry = evt.newValue == CheckboxState.Checked;
            });
            content.Add(pantryCheckbox);

            FMDialog.ShowCustom(
                this,
                "@UI:ADD_TO_MEAL_LOG",
                content,
                new FMDialogAction("@UI:TXT_CANCEL", null, false),
                new FMDialogAction("@UI:TXT_CONTINUE", () =>
                {
                    bool eatenOut = !isFromPantry;
                    int selectedMealType = (mealTypeDropdown.value != null && mealTypeDropdown.value.Any()) ? mealTypeDropdown.value.First() : 0;
                    _viewModel?.LogRecipe(selectedMealType, eatenOut);
                }, true)
            );
        }
        private void OnEditClicked() => _viewModel.Edit();
        private void OnAddToShoppingListClicked() => _ = SafeAddToShoppingListAsync();
        private void OnDeleteClicked()
        {
            FMDialog.ShowConfirm(this, "RECIPE_A_DELETE_CONFIRM_TITLE", "RECIPE_A_DELETE_CONFIRM_MSG",
                () => _ = SafeDeleteAsync(), null, AlertSemantic.Destructive);
        }

        private async System.Threading.Tasks.Task SafeAddToShoppingListAsync()
        {
            try
            {
                bool success = await _viewModel.AddIngredientsToShoppingListAsync();
                if (success && _viewModel.ErrorDetail == null)
                {
                    string msg = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "ITEM_ADDED_TO_SHOPPING_LIST");
                    if (string.IsNullOrEmpty(msg) || msg.StartsWith("No translation found"))
                    {
                        msg = "Ingredientes añadidos a la lista de la compra";
                    }
                    Toast.Build(this, msg, Unity.AppUI.Core.NotificationDuration.Short)
                        .SetStyle(NotificationStyle.Positive).Show();
                }
            }
            catch (Exception ex) { Debug.LogError($"[RecipeDetailScreen] SafeAddToShoppingListAsync: {ex}"); }
        }

        private async System.Threading.Tasks.Task SafeDeleteAsync()
        {
            try { await _viewModel.DeleteAsync(); }
            catch (Exception ex) { Debug.LogError($"[RecipeDetailScreen] SafeDeleteAsync: {ex}"); }
        }

        private UnityEngine.Accessibility.AccessibilityNode _btnLogNode;
        private UnityEngine.Accessibility.AccessibilityNode _btnAddToShoppingListNode;
        private UnityEngine.Accessibility.AccessibilityNode _btnEditNode;
        private UnityEngine.Accessibility.AccessibilityNode _btnDeleteNode;
        private UnityEngine.Accessibility.AccessibilityNode _btnWatchVideoNode;

        protected override void SetupAccessibilityNodes()
        {
            base.SetupAccessibilityNodes();
            if (_accessibilityHierarchy == null) return;
            if (_btnLog != null)
            {
                _btnLogNode = _accessibilityHierarchy.AddNode("Log recipe");
                _btnLogNode.role = UnityEngine.Accessibility.AccessibilityRole.Button;
            }
            if (_btnAddToShoppingList != null)
            {
                _btnAddToShoppingListNode = _accessibilityHierarchy.AddNode("Add to shopping list");
                _btnAddToShoppingListNode.role = UnityEngine.Accessibility.AccessibilityRole.Button;
            }
            if (_btnEdit != null && _btnEdit.enabledSelf)
            {
                _btnEditNode = _accessibilityHierarchy.AddNode("Edit");
                _btnEditNode.role = UnityEngine.Accessibility.AccessibilityRole.Button;
            }
            if (_btnDelete != null && _btnDelete.enabledSelf)
            {
                _btnDeleteNode = _accessibilityHierarchy.AddNode("Delete");
                _btnDeleteNode.role = UnityEngine.Accessibility.AccessibilityRole.Button;
            }
            if (_btnWatchVideo != null && _btnWatchVideo.enabledSelf)
            {
                _btnWatchVideoNode = _accessibilityHierarchy.AddNode("Watch video guide");
                _btnWatchVideoNode.role = UnityEngine.Accessibility.AccessibilityRole.Button;
            }
        }

        protected override void TeardownAccessibilityNodes()
        {
            _btnLogNode = null;
            _btnAddToShoppingListNode = null;
            _btnEditNode = null;
            _btnDeleteNode = null;
            _btnWatchVideoNode = null;
            base.TeardownAccessibilityNodes();
        }
    }
}
