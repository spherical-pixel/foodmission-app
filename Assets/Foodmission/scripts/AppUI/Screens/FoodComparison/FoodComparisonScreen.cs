using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation;
using Unity.AppUI.Navigation.Generated;
using Unity.AppUI.UI;
using UnityEngine.Localization.Settings;
using UnityEngine.Scripting;
using UnityEngine.UIElements;
using eu.foodmission.platform.Components;

namespace eu.foodmission.platform
{
    [Preserve]
    public class FoodComparisonScreen : NavigationScreenBase<FoodComparisonViewModel>
    {
        private TaskCompletionSource<bool> _rewardDismissed;
        private bool _isLeaving;

        protected override bool ApplySafeAreaBottom => false;
        protected override bool ApplySafeAreaLeft => false;
        protected override bool ApplySafeAreaRight => false;
        protected override bool ApplySafeAreaTop => false;
        protected override bool IsFixedContent => false;

        private VisualElement _stateEmpty;
        private VisualElement _stateDuel;
        private VisualElement _stateMatrix;

        // Duel elements
        private VisualElement _cardDuelA;
        private VisualElement _cardDuelB;
        private Text _cardDuelAEmoji;
        private Text _cardDuelBEmoji;
        private UnityEngine.UIElements.Image _cardDuelAImage;
        private UnityEngine.UIElements.Image _cardDuelBImage;
        private Text _cardDuelACat;
        private Text _cardDuelBCat;
        private Heading _cardDuelATitle;
        private Heading _cardDuelBTitle;
        private VisualElement _cardDuelACtaContainer;
        private VisualElement _cardDuelBCtaContainer;
        private VisualElement _cardDuelAResult;
        private VisualElement _cardDuelBResult;
        private Text _cardDuelAFootprint;
        private Text _cardDuelBFootprint;
        private Text _cardDuelABadge;
        private Text _cardDuelBBadge;
        private Text _cardDuelADisclaimer;
        private Text _cardDuelBDisclaimer;
        private VisualElement _vsBadge;

        private VisualElement _duelFeedbackBanner;
        private Text _duelFeedbackIcon;
        private Heading _duelFeedbackTitle;
        private Text _duelFeedbackBody;
        private FMButton _btnViewMatrix;
        private FMButton _btnGoShopping;
        private FMButton _btnPlaySampleDuel;
        private FMButton _btnResetComparison;
        private FMButton _btnDoneComparison;

        // Matrix elements
        private Heading _matrixTitleA;
        private Heading _matrixTitleB;
        private Text _matrixEmojiA;
        private Text _matrixEmojiB;
        private UnityEngine.UIElements.Image _matrixImageA;
        private UnityEngine.UIElements.Image _matrixImageB;
        private Text _matrixCatA;
        private Text _matrixCatB;
        private Text _matrixWinnerBadgeA;
        private Text _matrixWinnerBadgeB;
        private VisualElement _matrixCo2BoxA;
        private VisualElement _matrixCo2BoxB;
        private Text _matrixCo2A;
        private Text _matrixCo2B;
        private Text _matrixCo2DisclaimerA;
        private Text _matrixCo2DisclaimerB;
        private VisualElement _matrixScoreBoxA;
        private VisualElement _matrixScoreBoxB;
        private Text _matrixScoreA;
        private Text _matrixScoreB;
        private Text _matrixEcoScoreA;
        private Text _matrixEcoScoreB;
        private Text _matrixNutriScoreA;
        private Text _matrixNutriScoreB;
        private Text _matrixNovaA;
        private Text _matrixNovaB;
        private VisualElement _matrixRowEcoScore;
        private VisualElement _matrixRowNutriScore;
        private VisualElement _matrixRowNova;
        private Text _matrixTakeawayText;

        public FoodComparisonScreen()
        {
            InitializeComponent(App.current.services
                .GetRequiredService<ITemplateService>()
                .Get(TemplateAddresses.FoodComparison));

            CacheUIElements();
            RegisterEvents();
        }

        protected override void OnViewModelBound()
        {
            base.OnViewModelBound();
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            }
            UpdateVisualState();
            if (_viewModel != null && _viewModel.IsLoading)
            {
                ShowLoadingOverlay(_viewModel.LoadingText);
            }
        }

        protected override void OnViewModelUnbinding()
        {
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;

                // Leaving mid-load must not leave the global overlay blocking the next screen
                if (_viewModel.IsLoading)
                {
                    HideLoadingOverlay();
                }
            }
            base.OnViewModelUnbinding();
        }

        public override void OnEnter(NavController controller, NavDestination destination, Argument[] args)
        {
            base.OnEnter(controller, destination, args);
            _rewardDismissed = null;
            _isLeaving = false;

            string challengeCode = null;
            string mode = "proteins";
            string source = "shopping_list";

            if (args != null)
            {
                foreach (var a in args)
                {
                    if (a.name == "challengeCode") challengeCode = a.value;
                    else if (a.name == "mode") mode = a.value;
                    else if (a.name == "source") source = a.value;
                }
            }

            _ = _viewModel?.LoadComparisonDataAsync(challengeCode, mode, source);
            UpdateVisualState();
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_viewModel.IsLoading))
            {
                UpdateLoadingState();
                UpdateVisualState();
            }
            else if (e.PropertyName == nameof(_viewModel.DidacticFeedback))
            {
                UpdateFeedbackTexts();
            }
            else if (e.PropertyName == nameof(_viewModel.ErrorDetail))
            {
                UpdateApiErrorState();
            }
            else if (e.PropertyName == nameof(_viewModel.EarnedReward) && _viewModel.EarnedReward != null)
            {
                ShowChallengeRewardCelebration(_viewModel.EarnedReward);
            }
            else if (e.PropertyName == nameof(_viewModel.CurrentState) ||
                e.PropertyName == nameof(_viewModel.HasInsufficientProteins) ||
                e.PropertyName == nameof(_viewModel.IsGuessRevealed) ||
                e.PropertyName == nameof(_viewModel.ItemA) ||
                e.PropertyName == nameof(_viewModel.ItemB) ||
                e.PropertyName == nameof(_viewModel.ChallengeCode))
            {
                UpdateVisualState();
            }
        }

        private void UpdateFeedbackTexts()
        {
            if (_viewModel == null)
            {
                return;
            }

            if (_duelFeedbackBody != null)
            {
                _duelFeedbackBody.text = _viewModel.DidacticFeedback ?? string.Empty;
            }

            if (_matrixTakeawayText != null)
            {
                _matrixTakeawayText.text = string.IsNullOrEmpty(_viewModel.DidacticFeedback)
                    ? Localize("FC_TAKEAWAY_DEFAULT")
                    : _viewModel.DidacticFeedback;
            }
        }

        private void UpdateLoadingState()
        {
            if (_viewModel != null && _viewModel.IsLoading)
            {
                ShowLoadingOverlay(_viewModel.LoadingText);
            }
            else
            {
                HideLoadingOverlay();
            }
        }

        private void CacheUIElements()
        {
            var root = contentContainer;
            if (root == null) return;

            _stateEmpty = root.Q<VisualElement>("fc-state-empty");
            _stateDuel = root.Q<VisualElement>("fc-state-duel");
            _stateMatrix = root.Q<VisualElement>("fc-state-matrix");

            // Duel
            _cardDuelA = root.Q<VisualElement>("card-duel-a");
            _cardDuelB = root.Q<VisualElement>("card-duel-b");
            _cardDuelAEmoji = root.Q<Text>("card-duel-a-emoji");
            _cardDuelBEmoji = root.Q<Text>("card-duel-b-emoji");
            _cardDuelAImage = root.Q<UnityEngine.UIElements.Image>("card-duel-a-image");
            _cardDuelBImage = root.Q<UnityEngine.UIElements.Image>("card-duel-b-image");
            _cardDuelACat = root.Q<Text>("card-duel-a-category");
            _cardDuelBCat = root.Q<Text>("card-duel-b-category");
            _cardDuelATitle = root.Q<Heading>("card-duel-a-title");
            _cardDuelBTitle = root.Q<Heading>("card-duel-b-title");
            _cardDuelACtaContainer = root.Q<VisualElement>("card-duel-a-cta-container");
            _cardDuelBCtaContainer = root.Q<VisualElement>("card-duel-b-cta-container");
            _cardDuelAResult = root.Q<VisualElement>("card-duel-a-result");
            _cardDuelBResult = root.Q<VisualElement>("card-duel-b-result");
            _cardDuelAFootprint = root.Q<Text>("card-duel-a-footprint");
            _cardDuelBFootprint = root.Q<Text>("card-duel-b-footprint");
            _cardDuelADisclaimer = root.Q<Text>("card-duel-a-disclaimer");
            _cardDuelBDisclaimer = root.Q<Text>("card-duel-b-disclaimer");
            _cardDuelABadge = root.Q<Text>("card-duel-a-badge");
            _cardDuelBBadge = root.Q<Text>("card-duel-b-badge");
            _vsBadge = root.Q<VisualElement>("duel-vs-badge");
            _vsBadge?.BringToFront();

            _duelFeedbackBanner = root.Q<VisualElement>("duel-feedback-banner");
            _duelFeedbackIcon = root.Q<Text>("duel-feedback-icon");
            _duelFeedbackTitle = root.Q<Heading>("duel-feedback-title");
            _duelFeedbackBody = root.Q<Text>("duel-feedback-body");
            _btnViewMatrix = root.Q<FMButton>("btn-view-matrix");

            _btnGoShopping = root.Q<FMButton>("btn-go-to-shopping-list");
            _btnPlaySampleDuel = root.Q<FMButton>("btn-play-sample-duel");
            _btnResetComparison = root.Q<FMButton>("btn-reset-comparison");
            _btnDoneComparison = root.Q<FMButton>("btn-done-comparison");

            // Matrix
            _matrixTitleA = root.Q<Heading>("matrix-title-a");
            _matrixTitleB = root.Q<Heading>("matrix-title-b");
            _matrixEmojiA = root.Q<Text>("matrix-emoji-a");
            _matrixEmojiB = root.Q<Text>("matrix-emoji-b");
            _matrixImageA = root.Q<UnityEngine.UIElements.Image>("matrix-image-a");
            _matrixImageB = root.Q<UnityEngine.UIElements.Image>("matrix-image-b");
            _matrixCatA = root.Q<Text>("matrix-cat-a");
            _matrixCatB = root.Q<Text>("matrix-cat-b");
            _matrixWinnerBadgeA = root.Q<Text>("matrix-winner-badge-a");
            _matrixWinnerBadgeB = root.Q<Text>("matrix-winner-badge-b");

            _matrixCo2BoxA = root.Q<VisualElement>("matrix-co2-box-a");
            _matrixCo2BoxB = root.Q<VisualElement>("matrix-co2-box-b");
            _matrixCo2A = root.Q<Text>("matrix-co2-a");
            _matrixCo2B = root.Q<Text>("matrix-co2-b");
            _matrixCo2DisclaimerA = root.Q<Text>("matrix-co2-disclaimer-a");
            _matrixCo2DisclaimerB = root.Q<Text>("matrix-co2-disclaimer-b");

            _matrixScoreBoxA = root.Q<VisualElement>("matrix-score-box-a");
            _matrixScoreBoxB = root.Q<VisualElement>("matrix-score-box-b");
            _matrixScoreA = root.Q<Text>("matrix-score-a");
            _matrixScoreB = root.Q<Text>("matrix-score-b");

            _matrixEcoScoreA = root.Q<Text>("matrix-ecoscore-a");
            _matrixEcoScoreB = root.Q<Text>("matrix-ecoscore-b");
            _matrixNutriScoreA = root.Q<Text>("matrix-nutriscore-a");
            _matrixNutriScoreB = root.Q<Text>("matrix-nutriscore-b");
            _matrixNovaA = root.Q<Text>("matrix-nova-a");
            _matrixNovaB = root.Q<Text>("matrix-nova-b");
            _matrixRowEcoScore = root.Q<VisualElement>("matrix-row-ecoscore");
            _matrixRowNutriScore = root.Q<VisualElement>("matrix-row-nutriscore");
            _matrixRowNova = root.Q<VisualElement>("matrix-row-nova");
            _matrixTakeawayText = root.Q<Text>("matrix-takeaway-text");
        }

        private void RegisterEvents()
        {
            if (_btnGoShopping != null)
                _btnGoShopping.clicked += () => _viewModel?.NavigateToShoppingList();

            if (_btnPlaySampleDuel != null)
                _btnPlaySampleDuel.clicked += () => _viewModel?.LoadSampleDuel();

            if (_cardDuelA != null)
                _cardDuelA.RegisterCallback<ClickEvent>(evt => _ = _viewModel?.SubmitGuessAsync(0));

            if (_cardDuelB != null)
                _cardDuelB.RegisterCallback<ClickEvent>(evt => _ = _viewModel?.SubmitGuessAsync(1));

            if (_btnViewMatrix != null)
                _btnViewMatrix.clicked += () => _viewModel?.TransitionToMatrix();

            if (_btnResetComparison != null)
                _btnResetComparison.clicked += () => _viewModel?.ResetComparison();

            if (_btnDoneComparison != null)
            {
                _btnDoneComparison.clicked += OnDoneClicked;
            }
        }

        private async void OnDoneClicked()
        {
            if (_isLeaving || _viewModel == null)
            {
                return;
            }

            _isLeaving = true;
            try
            {
                if (!string.IsNullOrEmpty(_viewModel.ChallengeCode))
                {
                    // Waits for the request started by the guess, or retries a failed one
                    await _viewModel.CompleteChallengeAsync();
                    if (!_viewModel.IsChallengeCompleted)
                    {
                        // The error dialog is shown via ErrorDetail; stay so the user can retry
                        return;
                    }
                }

                // The reward dialog lives on this screen: leaving before it is dismissed would lose it
                if (_rewardDismissed != null)
                {
                    await _rewardDismissed.Task;
                }

                _navController?.PopBackStack();
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[{GetType().Name}] Done failed: {ex.Message}");
            }
            finally
            {
                _isLeaving = false;
            }
        }

        private void ShowChallengeRewardCelebration(ContentReward reward)
        {
            if (reward == null) return;
            var dismissed = new TaskCompletionSource<bool>();
            _rewardDismissed = dismissed;
            schedule.Execute(() =>
            {
                RewardCelebrationDialog.Show(reward, contextTitle: "@UI:CHALLENGE_REWARD_TITLE",
                    onDismiss: () => dismissed.TrySetResult(true));
            }).StartingIn(1200);
        }

        private void UpdateApiErrorState()
        {
            if (_viewModel?.ErrorDetail == null)
            {
                return;
            }

            FMDialog.ShowApiError(this, Localize("ERROR_TITLE"), _viewModel.ErrorDetail);
            _viewModel.ErrorDetail = null;
        }

        private static void SetDisplayed(VisualElement element, bool displayed)
        {
            if (element == null) return;
            element.style.display = displayed ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private static void SetScoreBadge(Text label, string score, string prefix)
        {
            if (label == null) return;
            string grade = (score ?? "unknown").ToLowerInvariant();
            label.text = string.IsNullOrEmpty(score) || score == "—" ? "?" : score.ToUpperInvariant();

            label.RemoveFromClassList($"{prefix}--a");
            label.RemoveFromClassList($"{prefix}--b");
            label.RemoveFromClassList($"{prefix}--c");
            label.RemoveFromClassList($"{prefix}--d");
            label.RemoveFromClassList($"{prefix}--e");
            label.RemoveFromClassList($"{prefix}--unknown");

            if (grade == "a" || grade == "b" || grade == "c" || grade == "d" || grade == "e")
                label.AddToClassList($"{prefix}--{grade}");
            else
                label.AddToClassList($"{prefix}--unknown");
        }

        // Numeric placeholders ({0:F1}) are formatted with the selected locale's culture
        private static string Localize(string key, params object[] args)
        {
            return args.Length == 0
                ? LocalizationSettings.StringDatabase.GetLocalizedString("UI", key)
                : LocalizationSettings.StringDatabase.GetLocalizedString("UI", key, args);
        }

        private static void SetNovaBadge(Text label, int? nova)
        {
            if (label == null) return;
            if (nova.HasValue && nova.Value >= 1 && nova.Value <= 4)
            {
                label.text = Localize("FC_NOVA_GROUP", nova.Value);
            }
            else
            {
                label.text = "?";
            }
        }

        private async void LoadFoodImageAsync(string url, UnityEngine.UIElements.Image imageElement, Text emojiElement)
        {
            if (imageElement == null) return;

            if (string.IsNullOrEmpty(url))
            {
                imageElement.image = null;
                imageElement.sprite = null;
                imageElement.userData = null;
                imageElement.style.display = DisplayStyle.None;
                if (emojiElement != null) emojiElement.style.display = DisplayStyle.Flex;
                return;
            }

            imageElement.userData = url;
            // Display emoji immediately while image loads asynchronously in background
            imageElement.style.display = DisplayStyle.None;
            if (emojiElement != null) emojiElement.style.display = DisplayStyle.Flex;

            try
            {
                var imageService = App.current?.services?.GetService(typeof(IImageService)) as IImageService;
                if (imageService == null)
                {
                    imageElement.image = null;
                    imageElement.sprite = null;
                    imageElement.style.display = DisplayStyle.None;
                    if (emojiElement != null) emojiElement.style.display = DisplayStyle.Flex;
                    return;
                }

                UnityEngine.Debug.Log($"[{GetType().Name}] Loading food image from: {url}");
                var texture = await imageService.LoadImageAsync(url);
                if ((string)imageElement.userData == url)
                {
                    if (texture != null)
                    {
                        var sprite = UnityEngine.Sprite.Create(texture, new UnityEngine.Rect(0, 0, texture.width, texture.height), UnityEngine.Vector2.zero);
                        imageElement.sprite = sprite;
                        imageElement.image = texture;
                        imageElement.style.width = StyleKeyword.Null;
                        imageElement.style.height = StyleKeyword.Null;
                        imageElement.style.display = DisplayStyle.Flex;
                        if (emojiElement != null) emojiElement.style.display = DisplayStyle.None;
                        UnityEngine.Debug.Log($"[{GetType().Name}] Successfully displayed image ({texture.width}x{texture.height}) for: {url}");
                    }
                    else
                    {
                        UnityEngine.Debug.LogWarning($"[{GetType().Name}] Image texture was null for: {url}");
                        imageElement.image = null;
                        imageElement.sprite = null;
                        imageElement.style.display = DisplayStyle.None;
                        if (emojiElement != null) emojiElement.style.display = DisplayStyle.Flex;
                    }
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"[{GetType().Name}] Could not load product image: {ex.Message}");
                if ((string)imageElement.userData == url)
                {
                    imageElement.image = null;
                    imageElement.sprite = null;
                    imageElement.style.display = DisplayStyle.None;
                    if (emojiElement != null) emojiElement.style.display = DisplayStyle.Flex;
                }
            }
        }

        private void UpdateVisualState()
        {
            if (_viewModel == null) return;

            // While the duel is being prepared no state is shown (the loading overlay covers the screen),
            // otherwise the default SelectionOrEmpty state flashes the "no proteins" card
            bool isReady = !_viewModel.IsLoading;
            bool isEmpty = isReady && _viewModel.CurrentState == ComparisonState.SelectionOrEmpty;
            bool isDuel = isReady && _viewModel.CurrentState == ComparisonState.NutriDuel;
            bool isMatrix = isReady && _viewModel.CurrentState == ComparisonState.FullComparison;

            SetDisplayed(_stateEmpty, isEmpty);
            SetDisplayed(_stateDuel, isDuel);
            SetDisplayed(_stateMatrix, isMatrix);

            // ── Duel State ──
            if (isDuel && _viewModel.ItemA != null && _viewModel.ItemB != null)
            {
                UnityEngine.Debug.Log($"[{GetType().Name}] Duel items: A='{_viewModel.ItemA.Name}' (img='{_viewModel.ItemA.ImageUrl ?? "null"}'), B='{_viewModel.ItemB.Name}' (img='{_viewModel.ItemB.ImageUrl ?? "null"}')");
                if (_cardDuelATitle != null) _cardDuelATitle.text = _viewModel.ItemA.Name;
                if (_cardDuelBTitle != null) _cardDuelBTitle.text = _viewModel.ItemB.Name;
                if (_cardDuelAEmoji != null) _cardDuelAEmoji.text = _viewModel.ItemA.Emoji ?? "🥩";
                if (_cardDuelBEmoji != null) _cardDuelBEmoji.text = _viewModel.ItemB.Emoji ?? "🫘";
                LoadFoodImageAsync(_viewModel.ItemA.ImageUrl, _cardDuelAImage, _cardDuelAEmoji);
                LoadFoodImageAsync(_viewModel.ItemB.ImageUrl, _cardDuelBImage, _cardDuelBEmoji);
                if (_cardDuelACat != null) _cardDuelACat.text = _viewModel.ItemA.Category;
                if (_cardDuelBCat != null) _cardDuelBCat.text = _viewModel.ItemB.Category;

                bool revealed = _viewModel.IsGuessRevealed;
                SetDisplayed(_cardDuelAResult, revealed);
                SetDisplayed(_cardDuelBResult, revealed);
                SetDisplayed(_duelFeedbackBanner, revealed);
                SetDisplayed(_cardDuelACtaContainer, !revealed);
                SetDisplayed(_cardDuelBCtaContainer, !revealed);
                SetDisplayed(_cardDuelABadge, revealed && _viewModel.WinnerIndex == 0);
                SetDisplayed(_cardDuelBBadge, revealed && _viewModel.WinnerIndex == 1);
                SetDisplayed(_vsBadge, !revealed);

                if (revealed)
                {
                    if (_cardDuelAFootprint != null) _cardDuelAFootprint.text = Localize("FC_FOOTPRINT_VALUE", _viewModel.ItemA.FootprintKg);
                    if (_cardDuelBFootprint != null) _cardDuelBFootprint.text = Localize("FC_FOOTPRINT_VALUE", _viewModel.ItemB.FootprintKg);
                    if (_cardDuelADisclaimer != null) _cardDuelADisclaimer.text = _viewModel.ItemA.FootprintDisclaimer;
                    if (_cardDuelBDisclaimer != null) _cardDuelBDisclaimer.text = _viewModel.ItemB.FootprintDisclaimer;

                    _cardDuelA?.EnableInClassList("fm-fc-duel-card--winner", _viewModel.WinnerIndex == 0);
                    _cardDuelA?.EnableInClassList("fm-fc-duel-card--loser", _viewModel.WinnerIndex == 1);
                    _cardDuelA?.EnableInClassList("fm-fc-duel-card--selected", _viewModel.SelectedItemIndex == 0);

                    _cardDuelB?.EnableInClassList("fm-fc-duel-card--winner", _viewModel.WinnerIndex == 1);
                    _cardDuelB?.EnableInClassList("fm-fc-duel-card--loser", _viewModel.WinnerIndex == 0);
                    _cardDuelB?.EnableInClassList("fm-fc-duel-card--selected", _viewModel.SelectedItemIndex == 1);

                    if (_duelFeedbackIcon != null)
                        _duelFeedbackIcon.text = _viewModel.IsGuessCorrect ? "🎉" : "💡";

                    if (_duelFeedbackTitle != null)
                        _duelFeedbackTitle.text = Localize(_viewModel.IsGuessCorrect ? "FC_FEEDBACK_CORRECT" : "FC_FEEDBACK_ALMOST");

                    UpdateFeedbackTexts();
                }
                else
                {
                    _cardDuelA?.RemoveFromClassList("fm-fc-duel-card--winner");
                    _cardDuelA?.RemoveFromClassList("fm-fc-duel-card--loser");
                    _cardDuelA?.RemoveFromClassList("fm-fc-duel-card--selected");

                    _cardDuelB?.RemoveFromClassList("fm-fc-duel-card--winner");
                    _cardDuelB?.RemoveFromClassList("fm-fc-duel-card--loser");
                    _cardDuelB?.RemoveFromClassList("fm-fc-duel-card--selected");
                }
            }

            // ── Matrix State ──
            if (isMatrix && _viewModel.ItemA != null && _viewModel.ItemB != null)
            {
                if (_matrixTitleA != null) _matrixTitleA.text = _viewModel.ItemA.Name;
                if (_matrixTitleB != null) _matrixTitleB.text = _viewModel.ItemB.Name;
                if (_matrixEmojiA != null) _matrixEmojiA.text = _viewModel.ItemA.Emoji ?? "🥩";
                if (_matrixEmojiB != null) _matrixEmojiB.text = _viewModel.ItemB.Emoji ?? "🫘";
                LoadFoodImageAsync(_viewModel.ItemA.ImageUrl, _matrixImageA, _matrixEmojiA);
                LoadFoodImageAsync(_viewModel.ItemB.ImageUrl, _matrixImageB, _matrixEmojiB);
                if (_matrixCatA != null) _matrixCatA.text = _viewModel.ItemA.Category;
                if (_matrixCatB != null) _matrixCatB.text = _viewModel.ItemB.Category;

                SetDisplayed(_matrixWinnerBadgeA, _viewModel.WinnerIndex == 0);
                SetDisplayed(_matrixWinnerBadgeB, _viewModel.WinnerIndex == 1);

                if (_matrixCo2A != null) _matrixCo2A.text = Localize("FC_FOOTPRINT_VALUE", _viewModel.ItemA.FootprintKg);
                if (_matrixCo2B != null) _matrixCo2B.text = Localize("FC_FOOTPRINT_VALUE", _viewModel.ItemB.FootprintKg);
                if (_matrixCo2DisclaimerA != null) _matrixCo2DisclaimerA.text = _viewModel.ItemA.FootprintDisclaimer;
                if (_matrixCo2DisclaimerB != null) _matrixCo2DisclaimerB.text = _viewModel.ItemB.FootprintDisclaimer;
                _matrixCo2BoxA?.EnableInClassList("fm-fc-metric-cell--winner", _viewModel.WinnerIndex == 0);
                _matrixCo2BoxB?.EnableInClassList("fm-fc-metric-cell--winner", _viewModel.WinnerIndex == 1);

                if (_matrixScoreA != null) _matrixScoreA.text = Localize("FC_SCORE_VALUE", _viewModel.ItemA.CompositeScore);
                if (_matrixScoreB != null) _matrixScoreB.text = Localize("FC_SCORE_VALUE", _viewModel.ItemB.CompositeScore);
                _matrixScoreBoxA?.EnableInClassList("fm-fc-metric-cell--winner", _viewModel.WinnerIndex == 0);
                _matrixScoreBoxB?.EnableInClassList("fm-fc-metric-cell--winner", _viewModel.WinnerIndex == 1);

                SetScoreBadge(_matrixEcoScoreA, _viewModel.ItemA.EcoScore, "fm-fc-score-badge");
                SetScoreBadge(_matrixEcoScoreB, _viewModel.ItemB.EcoScore, "fm-fc-score-badge");

                SetScoreBadge(_matrixNutriScoreA, _viewModel.ItemA.NutriScore, "fm-fc-score-badge");
                SetScoreBadge(_matrixNutriScoreB, _viewModel.ItemB.NutriScore, "fm-fc-score-badge");

                SetNovaBadge(_matrixNovaA, _viewModel.ItemA.NovaGroup);
                SetNovaBadge(_matrixNovaB, _viewModel.ItemB.NovaGroup);

                // Rows with no backend data for either food are hidden; if only one food has it, the other shows "?"
                SetDisplayed(_matrixRowEcoScore, !string.IsNullOrEmpty(_viewModel.ItemA.EcoScore) || !string.IsNullOrEmpty(_viewModel.ItemB.EcoScore));
                SetDisplayed(_matrixRowNutriScore, !string.IsNullOrEmpty(_viewModel.ItemA.NutriScore) || !string.IsNullOrEmpty(_viewModel.ItemB.NutriScore));
                SetDisplayed(_matrixRowNova, _viewModel.ItemA.NovaGroup.HasValue || _viewModel.ItemB.NovaGroup.HasValue);

                UpdateFeedbackTexts();
            }
        }
    }
}
