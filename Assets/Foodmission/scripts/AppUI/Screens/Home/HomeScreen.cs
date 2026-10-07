using System;
using System.Threading.Tasks;
using eu.foodmission.platform.Components;
using Unity.AppUI.Core;
using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation;
using Unity.AppUI.Navigation.Generated;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.Localization.Settings;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace eu.foodmission.platform
{
    [Preserve]
    class HomeScreen : NavigationScreenBase<HomeScreenViewModel>
    {
        public static void ResetSessionDeferredFlags()
        {
            HomePromptSession.Reset();
        }

        /// <summary>The user closed an onboarding step: don't remind them again until the next session.</summary>
        public static void DeferOnboardingReminder()
        {
            HomePromptSession.OnboardingDeferred = true;
        }
        private LinearProgress _healthProgress;
        private LinearProgress _sustainabilityProgress;
        private LinearProgress _knowledgeProgress;
        private CircularProgress _caloriesCircular;
        private Label _caloriesConsumedLabel;
        private Label _caloriesLeftLabel;

        private FMArrowStepper _periodStepper;
        private FMArrowStepper _scopeStepper;

        private static readonly string[] k_PeriodChoices = { "@UI:TODAY", "@UI:WEEK", "@UI:MONTH" };
        private static readonly string[] k_ScopeChoices = { "@UI:ME", "@UI:GROUP" };

        private AccessibilityNode _healthProgressNode;
        private AccessibilityNode _sustainabilityProgressNode;
        private AccessibilityNode _knowledgeProgressNode;
        private AccessibilityNode _caloriesNode;

        protected override bool ApplySafeAreaBottom => false;
        protected override bool ApplySafeAreaLeft => false;
        protected override bool ApplySafeAreaRight => false;
        protected override bool ApplySafeAreaTop => false;
        protected override bool IsFixedContent => false;


        private FMActiveQuestCard _activeQuestCard;
        private VisualElement _noActiveQuestBanner;
        private FMButton _btnChooseQuest;
        private FMNutriView _nutriView;

        private VisualElement _wheelsSection;
        private Heading _wheelsTitle;
        private Unity.AppUI.UI.Button _btnWheelsCustomize;
        private FMProgressWheelGrid _wheelsGrid;
        private VisualElement _wheelsMessage;
        private Text _wheelsMessageText;
        private FMButton _btnWheelsSurvey;
        private CircularProgress _wheelsLoading;
        private readonly System.Collections.Generic.List<AccessibilityNode> _wheelNodes = new System.Collections.Generic.List<AccessibilityNode>();
        private AccessibilityNode _wheelsCustomizeNode;
        private string _wheelNodesKey;
        // Each Home visit runs its prompts once; a new visit (or leaving Home) ends the previous run
        private int _promptGeneration;

        public HomeScreen()
        {
            InitializeComponent(App.current.services
                .GetRequiredService<ITemplateService>()
                .Get(TemplateAddresses.Home));
            CacheUIElements();
        }

        public override void OnEnter(NavController controller, NavDestination destination, Argument[] args)
        {
            base.OnEnter(controller, destination, args);
            _ = _viewModel?.LoadActiveQuestAsync();
            RefreshActiveQuestWidget();
            _nutriView?.RefreshView();
            //SetupFoodComparisonDebugPanel();
            //SetupRewardDebugButton();
        }

        private void CacheUIElements()
        {
            _healthProgress = contentContainer.Q<LinearProgress>("health-progress");
            _sustainabilityProgress = contentContainer.Q<LinearProgress>("sustainability-progress");
            _knowledgeProgress = contentContainer.Q<LinearProgress>("knowledge-progress");
            _caloriesCircular = contentContainer.Q<CircularProgress>("calories-circular");
            _caloriesConsumedLabel = contentContainer.Q<Label>("calories-consumed");
            _caloriesLeftLabel = contentContainer.Q<Label>("calories-left");

            _activeQuestCard = contentContainer.Q<FMActiveQuestCard>("active-quest-card");
            _noActiveQuestBanner = contentContainer.Q<VisualElement>("no-active-quest-banner");
            _btnChooseQuest = contentContainer.Q<FMButton>("btn-choose-quest");

            _periodStepper = contentContainer.Q<FMArrowStepper>("period-stepper");
            _scopeStepper = contentContainer.Q<FMArrowStepper>("scope-stepper");
            _nutriView = contentContainer.Q<FMNutriView>("nutri-render");

            _wheelsSection = contentContainer.Q<VisualElement>("wheels-section");
            _wheelsTitle = contentContainer.Q<Heading>("wheels-title");
            _btnWheelsCustomize = contentContainer.Q<Unity.AppUI.UI.Button>("btn-wheels-customize");
            _wheelsGrid = contentContainer.Q<FMProgressWheelGrid>("wheels-grid");
            _wheelsMessage = contentContainer.Q<VisualElement>("wheels-message");
            _wheelsMessageText = contentContainer.Q<Text>("wheels-message-text");
            _btnWheelsSurvey = contentContainer.Q<FMButton>("btn-wheels-survey");
            _wheelsLoading = contentContainer.Q<CircularProgress>("wheels-loading");
        }

        protected override void OnViewModelBound()
        {
            base.OnViewModelBound();
            CacheUIElements();
            RegisterEvents();
            RefreshStats();
            SetupSteppers();
            RefreshActiveQuestWidget();
            _ = _viewModel?.LoadActiveQuestAsync();
            RenderProgressWheels();
            _viewModel?.RefreshProgressWheels();

            StartPromptRun();
        }

        private sealed class PromptHost : IHomePromptHost
        {
            private readonly HomeScreen _screen;
            private readonly HomeScreenViewModel _viewModel;
            private readonly int _generation;

            public PromptHost(HomeScreen screen, HomeScreenViewModel viewModel, int generation)
            {
                _screen = screen;
                _viewModel = viewModel;
                _generation = generation;
            }

            public bool IsActive => _screen._promptGeneration == _generation && _screen._viewModel == _viewModel && _screen.panel != null;

            public VisualElement DialogAnchor => _screen;

            public void RefreshActiveQuestWidget() => _screen.RefreshActiveQuestWidget();

            public void NavigateToAuth() => _screen._navController?.Navigate(Actions.go_to_auth);
        }

        private void StartPromptRun()
        {
            HomeScreenViewModel viewModel = _viewModel;
            if (viewModel == null)
            {
                return;
            }

            viewModel.RecordPilotHomeEntry();
            var host = new PromptHost(this, viewModel, ++_promptGeneration);
            var whatsNew = App.current?.services?.GetService<IWhatsNewService>();
            var coordinator = new HomePromptCoordinator(HomePrompts.Create(viewModel, new HomePromptRunState(), whatsNew), new RewardCelebrationGate());
            _ = coordinator.RunAsync(host);
        }

        private void SetupRewardDebugButton()
        {
            var root = contentContainer.Q<VisualElement>("root") ?? contentContainer;
            if (root == null) return;

            var existing = root.Q<VisualElement>("reward-debug-container");
            if (existing != null)
            {
                existing.parent?.Remove(existing);
            }

            var debugContainer = new VisualElement();
            debugContainer.name = "reward-debug-container";
            debugContainer.style.marginTop = 16;
            debugContainer.style.marginBottom = 16;
            debugContainer.style.marginLeft = 20;
            debugContainer.style.marginRight = 20;
            debugContainer.style.alignItems = Align.Center;

            var btn = new FMButton
            {
                title = "🎁 Probar Recompensas (Debug)",
                variant = ButtonVariant.Accent,
                size = Size.L
            };
            btn.style.width = Length.Percent(100);
            btn.clicked += () =>
            {
                var simulatedReward = new ContentReward
                {
                    xp = 120,
                    points = 51,

                    /*,
                    badgeId = "Maestro Sostenible",
                    avatarItem = "Gorro de Chef Verde",
                    petItem = "Collar Ecológico",
                    collectible = "Trofeo Huella Cero"*/
                };

                var simulatedQuestItem = new RewardPresentationItem
                {
                    Type = RewardType.QuestUnlocked,
                    Title = "@UI:QUEST_UNLOCKED_TITLE",
                    Subtitle = "Reducir el consumo de carne roja II",
                    IconEmoji = "🔓"
                };

                RewardCelebrationDialog.Show(
                    simulatedReward,
                    contextTitle: "@UI:QUEST_REWARD_TITLE",
                    extraItem: simulatedQuestItem
                );
            };

            debugContainer.Add(btn);

            if (_activeQuestCard != null && _activeQuestCard.parent != null)
            {
                int index = _activeQuestCard.parent.IndexOf(_activeQuestCard);
                _activeQuestCard.parent.Insert(index, debugContainer);
            }
            else
            {
                root.Add(debugContainer);
            }
        }

        private void SetupFoodComparisonDebugPanel()
        {
            var root = contentContainer.Q<VisualElement>("root") ?? contentContainer;
            if (root == null) return;

            var existing = root.Q<VisualElement>("test-food-comparison-panel");
            if (existing != null)
            {
                existing.parent?.Remove(existing);
            }

            var panel = new VisualElement();
            panel.name = "test-food-comparison-panel";
            panel.style.marginTop = 16;
            panel.style.marginBottom = 16;
            panel.style.marginLeft = 20;
            panel.style.marginRight = 20;
            panel.style.paddingTop = 12;
            panel.style.paddingBottom = 12;
            panel.style.paddingLeft = 12;
            panel.style.paddingRight = 12;
            panel.style.borderTopLeftRadius = 12;
            panel.style.borderTopRightRadius = 12;
            panel.style.borderBottomLeftRadius = 12;
            panel.style.borderBottomRightRadius = 12;
            panel.style.borderTopWidth = 1;
            panel.style.borderBottomWidth = 1;
            panel.style.borderLeftWidth = 1;
            panel.style.borderRightWidth = 1;
            panel.style.borderTopColor = new Color(0f, 0.545f, 0.227f, 0.5f);
            panel.style.borderBottomColor = new Color(0f, 0.545f, 0.227f, 0.5f);
            panel.style.borderLeftColor = new Color(0f, 0.545f, 0.227f, 0.5f);
            panel.style.borderRightColor = new Color(0f, 0.545f, 0.227f, 0.5f);
            panel.style.backgroundColor = new Color(0.12f, 0.12f, 0.12f, 0.4f);

            var header = new Text
            {
                text = "🧪 Comparador de Alimentos (Test Modos)"
            };
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.color = new Color(0.1f, 0.85f, 0.35f);
            header.style.marginBottom = 8;
            header.style.unityTextAlign = TextAnchor.MiddleCenter;
            panel.Add(header);

            var grid = new VisualElement();
            grid.style.flexDirection = FlexDirection.Row;
            grid.style.flexWrap = Wrap.Wrap;
            grid.style.justifyContent = Justify.SpaceBetween;

            var btnShopping = new FMButton { title = "🛒 Duelo Lista", variant = ButtonVariant.Accent, size = Size.S };
            btnShopping.style.width = Length.Percent(48);
            btnShopping.style.marginBottom = 8;
            btnShopping.clicked += () => _viewModel?.NavigateToFoodComparison("proteins", "shopping_list");
            grid.Add(btnShopping);

            var btnSample = new FMButton { title = "⚡ Duelo Ejemplos", variant = ButtonVariant.Default, size = Size.S };
            btnSample.style.width = Length.Percent(48);
            btnSample.style.marginBottom = 8;
            btnSample.clicked += () => _viewModel?.NavigateToFoodComparison("sample", "sample");
            grid.Add(btnSample);

            var btnMatrix = new FMButton { title = "📊 Ver Matriz", variant = ButtonVariant.Default, size = Size.S };
            btnMatrix.style.width = Length.Percent(48);
            btnMatrix.clicked += () => _viewModel?.NavigateToFoodComparison("matrix", "matrix");
            grid.Add(btnMatrix);

            var btnEmpty = new FMButton { title = "🥑 Modo Vacío", variant = ButtonVariant.Default, size = Size.S };
            btnEmpty.style.width = Length.Percent(48);
            btnEmpty.clicked += () => _viewModel?.NavigateToFoodComparison("empty", "empty");
            grid.Add(btnEmpty);

            panel.Add(grid);

            if (_activeQuestCard != null && _activeQuestCard.parent != null)
            {
                int index = _activeQuestCard.parent.IndexOf(_activeQuestCard);
                _activeQuestCard.parent.Insert(index, panel);
            }
            else
            {
                root.Add(panel);
            }
        }

        private async void SetupPilotDebugPanel()
        {
            var root = contentContainer.Q<VisualElement>("root") ?? contentContainer;
            if (root == null || _viewModel == null) return;

            // Remove existing debug card if re-entering
            var existing = root.Q<VisualElement>("pilot-debug-panel");
            if (existing != null)
            {
                root.Remove(existing);
            }

            var debugCard = new ExVisualElement();
            debugCard.name = "pilot-debug-panel";
            debugCard.AddToClassList("box-background");
            debugCard.AddToClassList("fm-shadow-wrapper");
            debugCard.style.marginTop = 20;
            debugCard.style.marginBottom = 30;
            debugCard.style.marginLeft = 16;
            debugCard.style.marginRight = 16;
            debugCard.style.paddingTop = 16;
            debugCard.style.paddingBottom = 16;
            debugCard.style.paddingLeft = 16;
            debugCard.style.paddingRight = 16;
            debugCard.style.flexDirection = FlexDirection.Column;
            debugCard.style.borderTopWidth = 2;
            debugCard.style.borderBottomWidth = 2;
            debugCard.style.borderLeftWidth = 2;
            debugCard.style.borderRightWidth = 2;
            debugCard.style.borderTopColor = new StyleColor(new Color(0.15f, 0.65f, 0.85f, 0.9f));
            debugCard.style.borderBottomColor = new StyleColor(new Color(0.15f, 0.65f, 0.85f, 0.9f));
            debugCard.style.borderLeftColor = new StyleColor(new Color(0.15f, 0.65f, 0.85f, 0.9f));
            debugCard.style.borderRightColor = new StyleColor(new Color(0.15f, 0.65f, 0.85f, 0.9f));
            debugCard.style.borderTopLeftRadius = 12;
            debugCard.style.borderTopRightRadius = 12;
            debugCard.style.borderBottomLeftRadius = 12;
            debugCard.style.borderBottomRightRadius = 12;

            var title = new Unity.AppUI.UI.Text
            {
                text = "🧪 Dev Test Panel: Pilot Surveys",
                size = TextSize.M
            };
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom = 6;
            debugCard.Add(title);

            var state = _viewModel.GetPilotCycleState();
            int currentDays = _viewModel.GetPilotActiveDays();
            int currentDaysSinceStart = _viewModel.GetPilotDaysSinceStart();
            int currentCycle = state?.currentCycle ?? 1;
            string country = _viewModel.GetCurrentUserCountry();
            bool isPilot = _viewModel.IsUserInPilotCountry();
            bool hasConsent = await _viewModel.HasAcceptedPilotConsentAsync();

            var statusLabel = new Unity.AppUI.UI.Text();
            statusLabel.size = TextSize.S;
            statusLabel.style.whiteSpace = WhiteSpace.Normal;
            statusLabel.style.marginBottom = 10;

            Action refreshStatus = () =>
            {
                var s = _viewModel.GetPilotCycleState();
                string c = _viewModel.GetCurrentUserCountry();
                bool ip = _viewModel.IsUserInPilotCountry();
                bool bypass = _viewModel.DebugBypassEligibility;
                statusLabel.text = $"Cycle: {s?.currentCycle ?? 1} | Active days: {s?.activeDatesInCycle.Count ?? 0} | Days elapsed: {_viewModel.GetPilotDaysSinceStart()}\n" +
                                   $"Country: '{c}' (Pilot: {ip}) | Bypass Eligibility: {(bypass ? "YES" : "NO")}\n" +
                                   $"Completed: [{(s?.completedSlugsInCycle != null && s.completedSlugsInCycle.Count > 0 ? string.Join(", ", s.completedSlugsInCycle) : "None")}]";
            };

            refreshStatus();
            debugCard.Add(statusLabel);

            // Row 1: Helpers for Country and Consent
            var helpersRow = new VisualElement();
            helpersRow.style.flexDirection = FlexDirection.Row;
            helpersRow.style.justifyContent = Justify.SpaceBetween;
            helpersRow.style.marginBottom = 10;

            var btnBypass = new FMButton
            {
                title = _viewModel.DebugBypassEligibility ? "Bypass: ACTIVE" : "Activate Bypass",
                size = Size.S,
                variant = _viewModel.DebugBypassEligibility ? ButtonVariant.Accent : ButtonVariant.Default
            };
            btnBypass.clicked += () =>
            {
                _viewModel.DebugBypassEligibility = !_viewModel.DebugBypassEligibility;
                btnBypass.title = _viewModel.DebugBypassEligibility ? "Bypass: ACTIVE" : "Activate Bypass";
                btnBypass.variant = _viewModel.DebugBypassEligibility ? ButtonVariant.Accent : ButtonVariant.Default;
                refreshStatus();
            };
            helpersRow.Add(btnBypass);

            var btnSimulateDE = new FMButton
            {
                title = "Set Country 'DE' + Consent.",
                size = Size.S,
                variant = ButtonVariant.Default
            };
            btnSimulateDE.clicked += async () =>
            {
                _viewModel.SetDebugUserCountry("de");
                await _viewModel.AcceptPilotConsentAsync();
                refreshStatus();
            };
            helpersRow.Add(btnSimulateDE);
            debugCard.Add(helpersRow);

            // Row 2 for changing days
            var stepperRow = new VisualElement();
            stepperRow.style.flexDirection = FlexDirection.Row;
            stepperRow.style.alignItems = Align.Center;
            stepperRow.style.marginBottom = 12;

            var stepperLabel = new Unity.AppUI.UI.Text { text = "Simulate Day: ", size = TextSize.S };
            stepperLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            stepperRow.Add(stepperLabel);

            var choices = new string[]
            {
                "Day 1 (no survey)",
                "Day 2 (second-use)",
                "Day 3 (third-use)",
                "Day 4 (fourth-use)",
                "Day 5 (fifth-use)",
                "Day 6 (sixth-use)",
                "Day 7 (seventh)",
                "Day 8 + 30d (after-1-mt...)",
                "Day 9 + 30d (after-1-m...)",
                "Day 10 + 30d (after-1-m...)",
                "Day 11 + 30d (end)"
            };

            int selectedIndex = Math.Clamp(currentDays - 1, 0, choices.Length - 1);

            var dayStepper = new FMArrowStepper
            {
                Choices = choices,
                SelectedIndex = selectedIndex
            };
            dayStepper.style.flexGrow = 1;
            dayStepper.valueChanged += (sender, evt) =>
            {
                int dayIndex = evt.newValue;
                int activeDays = dayIndex + 1;
                int daysSinceStart = (dayIndex >= 7) ? 35 : activeDays;
                _viewModel.SetPilotDebugDays(activeDays, daysSinceStart);
                refreshStatus();
            };
            stepperRow.Add(dayStepper);
            debugCard.Add(stepperRow);

            // Row 3: Action Buttons
            var btnRow = new VisualElement();
            btnRow.style.flexDirection = FlexDirection.Row;
            btnRow.style.justifyContent = Justify.SpaceBetween;

            var btnCheck = new FMButton
            {
                title = "Evaluate Survey",
                size = Size.S,
                variant = ButtonVariant.Accent
            };
            btnCheck.clicked += () =>
            {
                HomeScreenViewModel viewModel = _viewModel;
                if (viewModel == null)
                {
                    return;
                }
                var host = new PromptHost(this, viewModel, ++_promptGeneration);
                _ = new HomePromptCoordinator(new IHomePrompt[] { new PilotSurveyPrompt(viewModel) }, new RewardCelebrationGate()).RunAsync(host);
            };
            btnRow.Add(btnCheck);

            var btnResetSurveys = new FMButton
            {
                title = "Reset Surveys Cycle",
                size = Size.S,
                variant = ButtonVariant.Default
            };
            btnResetSurveys.clicked += () =>
            {
                _viewModel.ResetPilotCycleSurveys();
                refreshStatus();
            };
            btnRow.Add(btnResetSurveys);

            debugCard.Add(btnRow);
            root.Add(debugCard);
        }

        private void RegisterEvents()
        {
            if (_activeQuestCard != null)
            {
                _activeQuestCard.Clicked += OnActiveQuestClicked;
                _activeQuestCard.QuickMealClicked += OnActiveQuestQuickMealClicked;
                _activeQuestCard.CheckInClicked += OnActiveQuestCheckInClicked;
            }
            if (_btnChooseQuest != null) _btnChooseQuest.clicked += OnChooseQuestClicked;
            if (_nutriView != null) _nutriView.OnClick = () => _navController?.Navigate(Actions.go_to_nutri_editor);
            if (_viewModel != null) _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            if (_btnWheelsCustomize != null)
            {
                _btnWheelsCustomize.clicked += OnWheelsCustomizeClicked;
            }
            if (_btnWheelsSurvey != null)
            {
                _btnWheelsSurvey.clicked += OnWheelsSurveyClicked;
            }
            if (_viewModel != null)
            {
                _viewModel.ProgressWheelsChanged += RenderProgressWheels;
            }
        }

        private void UnregisterEvents()
        {
            if (_activeQuestCard != null)
            {
                _activeQuestCard.Clicked -= OnActiveQuestClicked;
                _activeQuestCard.QuickMealClicked -= OnActiveQuestQuickMealClicked;
                _activeQuestCard.CheckInClicked -= OnActiveQuestCheckInClicked;
            }
            if (_btnChooseQuest != null) _btnChooseQuest.clicked -= OnChooseQuestClicked;
            if (_nutriView != null) _nutriView.OnClick = null;
            if (_viewModel != null) _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            if (_btnWheelsCustomize != null)
            {
                _btnWheelsCustomize.clicked -= OnWheelsCustomizeClicked;
            }
            if (_btnWheelsSurvey != null)
            {
                _btnWheelsSurvey.clicked -= OnWheelsSurveyClicked;
            }
            if (_viewModel != null)
            {
                _viewModel.ProgressWheelsChanged -= RenderProgressWheels;
            }
        }

        private void OnActiveQuestClicked()
        {
            _viewModel?.OpenCurrentQuest();
        }

        private void OnActiveQuestQuickMealClicked()
        {
            _viewModel?.NavigateToQuickMealLog();
        }

        private void OnActiveQuestCheckInClicked()
        {
            _viewModel?.OpenCheckIn();
        }

        private void OnChooseQuestClicked()
        {
            _viewModel?.NavigateToQuests();
        }

        private void OnViewModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_viewModel.HasActiveQuest) ||
                e.PropertyName == nameof(_viewModel.CurrentQuestTitle) ||
                e.PropertyName == nameof(_viewModel.CurrentQuestActivityStates))
            {
                RefreshActiveQuestWidget();
            }
        }

        private void RefreshActiveQuestWidget()
        {
            if (_viewModel == null) return;

            if (_viewModel.HasActiveQuest)
            {
                if (_activeQuestCard != null)
                {
                    _activeQuestCard.style.display = DisplayStyle.Flex;
                    _activeQuestCard.Setup(_viewModel.CurrentQuestTitle, _viewModel.CurrentQuestActivityStates);
                }
                if (_noActiveQuestBanner != null)
                {
                    _noActiveQuestBanner.style.display = DisplayStyle.None;
                }
            }
            else
            {
                if (_activeQuestCard != null)
                {
                    _activeQuestCard.style.display = DisplayStyle.None;
                }
                if (_noActiveQuestBanner != null)
                {
                    _noActiveQuestBanner.style.display = DisplayStyle.Flex;
                }
            }
        }

        private void OnOpenQuizOpen()
        {
            Debug.Log("OnOpenQuizOpen -> Navigating to QuizzesScreen");
            _navController.Navigate(Actions.go_to_quizzes);
        }


        private void SetupSteppers()
        {
            if (_periodStepper != null)
            {
                _periodStepper.Choices = k_PeriodChoices;
                _periodStepper.SelectedIndex = _viewModel.SelectedTimePeriod switch
                {
                    TimePeriod.TODAY => 0,
                    TimePeriod.WEEK => 1,
                    TimePeriod.MONTH => 2,
                    _ => 0
                };
                _periodStepper.RegisterValueChangedCallback(OnPeriodChanged);
            }

            if (_scopeStepper != null)
            {
                _scopeStepper.Choices = k_ScopeChoices;
                _scopeStepper.SelectedIndex = _viewModel.SelectedUserScope switch
                {
                    UserScope.ME => 0,
                    UserScope.GROUP => 1,
                    _ => 0
                };
                _scopeStepper.RegisterValueChangedCallback(OnScopeChanged);
            }
        }

        private void OnPeriodChanged(object sender, ChangeEvent<int> evt)
        {
            TimePeriod selectedPeriod = evt.newValue switch
            {
                0 => TimePeriod.TODAY,
                1 => TimePeriod.WEEK,
                2 => TimePeriod.MONTH,
                _ => TimePeriod.TODAY
            };
            _viewModel.SetTimePeriod(selectedPeriod);
        }

        private void OnScopeChanged(object sender, ChangeEvent<int> evt)
        {
            UserScope selectedScope = evt.newValue switch
            {
                0 => UserScope.ME,
                1 => UserScope.GROUP,
                _ => UserScope.ME
            };
            _viewModel.SetUserScope(selectedScope);
        }

        private void RefreshStats()
        {
            if (_viewModel == null) return;

            if (_healthProgress != null) _healthProgress.value = _viewModel.HealthProgress;
            if (_sustainabilityProgress != null) _sustainabilityProgress.value = _viewModel.SustainabilityProgress;
            if (_knowledgeProgress != null) _knowledgeProgress.value = _viewModel.KnowledgeProgress;

            int total = _viewModel.CaloriesConsumed + _viewModel.CaloriesLeft;
            if (_caloriesCircular != null)
            {
                _caloriesCircular.value = total > 0 ? (float)_viewModel.CaloriesConsumed / total : 0f;
            }

            if (_caloriesConsumedLabel != null) _caloriesConsumedLabel.text = _viewModel.CaloriesConsumed.ToString();
            if (_caloriesLeftLabel != null) _caloriesLeftLabel.text = _viewModel.CaloriesLeft.ToString();
        }

        protected override void OnViewModelUnbinding()
        {
            _promptGeneration++; // ends the running prompt run
            UnregisterEvents();

            _healthProgress = null;
            _sustainabilityProgress = null;
            _knowledgeProgress = null;
            _caloriesCircular = null;
            _caloriesConsumedLabel = null;
            _caloriesLeftLabel = null;
            _periodStepper = null;
            _scopeStepper = null;


            _activeQuestCard = null;
            _noActiveQuestBanner = null;
            _btnChooseQuest = null;

            _wheelsSection = null;
            _wheelsTitle = null;
            _btnWheelsCustomize = null;
            _wheelsGrid = null;
            _wheelsMessage = null;
            _wheelsMessageText = null;
            _btnWheelsSurvey = null;
            _wheelsLoading = null;

            base.OnViewModelUnbinding();
        }

        // --------------------------------------------------------------------
        // Accessibility
        // --------------------------------------------------------------------

        protected override void SetupAccessibilityNodes()
        {
            base.SetupAccessibilityNodes();
            if (_accessibilityHierarchy == null) return;

            var h = _accessibilityHierarchy;

            _periodStepper?.CreateAccessibilityNode(h, "Time period");
            _scopeStepper?.CreateAccessibilityNode(h, "Scope");

            if (_healthProgress != null)
            {
                _healthProgressNode = h.AddNode("Health progress");
                _healthProgressNode.role = AccessibilityRole.StaticText;
                _healthProgressNode.value = $"{_viewModel?.HealthProgress * 100:F0}%";
                _healthProgressNode.frameGetter = MakeElementFrameGetter(_healthProgress);
            }

            if (_sustainabilityProgress != null)
            {
                _sustainabilityProgressNode = h.AddNode("Sustainability progress");
                _sustainabilityProgressNode.role = AccessibilityRole.StaticText;
                _sustainabilityProgressNode.value = $"{_viewModel?.SustainabilityProgress * 100:F0}%";
                _sustainabilityProgressNode.frameGetter = MakeElementFrameGetter(_sustainabilityProgress);
            }

            if (_knowledgeProgress != null)
            {
                _knowledgeProgressNode = h.AddNode("Knowledge progress");
                _knowledgeProgressNode.role = AccessibilityRole.StaticText;
                _knowledgeProgressNode.value = $"{_viewModel?.KnowledgeProgress * 100:F0}%";
                _knowledgeProgressNode.frameGetter = MakeElementFrameGetter(_knowledgeProgress);
            }

            if (_caloriesCircular != null)
            {
                string caloriesText = _viewModel != null
                    ? $"{_viewModel.CaloriesConsumed} consumed, {_viewModel.CaloriesLeft} left"
                    : "Calories";
                _caloriesNode = h.AddNode(caloriesText);
                _caloriesNode.role = AccessibilityRole.StaticText;
                _caloriesNode.frameGetter = MakeElementFrameGetter(_caloriesCircular);
            }

            RefreshWheelAccessibilityNodes();
        }

        protected override void TeardownAccessibilityNodes()
        {
            _healthProgressNode = null;
            _sustainabilityProgressNode = null;
            _knowledgeProgressNode = null;
            _caloriesNode = null;
            _wheelNodes.Clear();
            _wheelsCustomizeNode = null;
            _wheelNodesKey = null;

            _periodStepper?.DestroyAccessibilityNode();
            _scopeStepper?.DestroyAccessibilityNode();

            base.TeardownAccessibilityNodes();
        }

        private void RenderProgressWheels()
        {
            if (_wheelsSection == null || _viewModel == null)
            {
                return;
            }

            ProgressWheelSectionModel model = _viewModel.GetProgressWheelSection();
            bool visible = model.State != ProgressWheelSectionState.Hidden;
            _wheelsSection.EnableInClassList("fm-wheels-section--hidden", !visible);
            if (!visible)
            {
                RefreshWheelAccessibilityNodes();
                return;
            }

            if (_wheelsTitle != null)
            {
                _wheelsTitle.text = ProgressWheelText.SectionHeader(model.Segment);
            }
            _btnWheelsCustomize?.EnableInClassList("fm-wheels-section__part--hidden", model.All.Count == 0);

            bool showGrid = model.State == ProgressWheelSectionState.Grid;
            _wheelsGrid?.EnableInClassList("fm-wheels-section__part--hidden", !showGrid);
            if (_wheelsGrid != null)
            {
                _wheelsGrid.ForceSingleColumn = model.ForceSingleColumn;
                _wheelsGrid.SetWheels(showGrid ? model.Visible : System.Array.Empty<ProgressWheel>());
            }

            bool showMessage = model.State == ProgressWheelSectionState.AllHidden || model.State == ProgressWheelSectionState.SurveyCta;
            _wheelsMessage?.EnableInClassList("fm-wheels-section__part--hidden", !showMessage);
            if (_wheelsMessageText != null && showMessage)
            {
                _wheelsMessageText.text = ProgressWheelText.Localize(model.State == ProgressWheelSectionState.SurveyCta ? "WHEELS_SURVEY_CTA_TEXT" : "WHEELS_ALL_HIDDEN");
            }
            _btnWheelsSurvey?.EnableInClassList("fm-wheels-section__part--hidden", model.State != ProgressWheelSectionState.SurveyCta);

            _wheelsLoading?.EnableInClassList("fm-wheels-section__part--hidden", model.State != ProgressWheelSectionState.Loading);

            RefreshWheelAccessibilityNodes();
        }

        private void OnWheelsSurveyClicked()
        {
            _viewModel?.NavigateToOnboardingSurvey();
        }

        private void OnWheelsCustomizeClicked()
        {
            if (_viewModel == null)
            {
                return;
            }

            // Captured so Save still works if the screen unbinds while the dialog is open.
            HomeScreenViewModel viewModel = _viewModel;
            ProgressWheelSectionModel model = viewModel.GetProgressWheelSection();
            var hidden = new System.Collections.Generic.HashSet<string>(model.Hidden);
            var toggles = new System.Collections.Generic.Dictionary<string, Unity.AppUI.UI.Toggle>();
            var content = new VisualElement();

            void UpdateLocks()
            {
                var enabled = new System.Collections.Generic.List<string>();
                foreach (var pair in toggles)
                {
                    if (pair.Value.value)
                    {
                        enabled.Add(pair.Key);
                    }
                }
                string locked = ProgressWheelSection.LockedKind(enabled);
                foreach (var pair in toggles)
                {
                    pair.Value.SetEnabled(pair.Key != locked);
                }
            }

            foreach (ProgressWheel wheel in ProgressWheelSection.CustomizableWheels(model.All))
            {
                var toggle = new Unity.AppUI.UI.Toggle { label = ProgressWheelText.Name(wheel) };
                toggle.AddToClassList("fm-wheels-customize__toggle");
                toggle.SetValueWithoutNotify(!hidden.Contains(wheel.kind));
                toggle.RegisterValueChangedCallback(_ => UpdateLocks());
                toggles[wheel.kind] = toggle;
                content.Add(toggle);
            }
            UpdateLocks();

            FMDialog.ShowCustom(
                this,
                "@UI:WHEELS_CUSTOMIZE_TITLE",
                content,
                new FMDialogAction("@UI:TXT_CANCEL", null),
                new FMDialogAction("@UI:SAVE", () =>
                {
                    var newHidden = new System.Collections.Generic.List<string>();
                    foreach (var pair in toggles)
                    {
                        if (!pair.Value.value)
                        {
                            newHidden.Add(pair.Key);
                        }
                    }
                    _ = viewModel.SaveHiddenWheelsAsync(newHidden);
                }, ButtonVariant.Accent));
        }

        private void RefreshWheelAccessibilityNodes()
        {
            if (_accessibilityHierarchy == null)
            {
                return;
            }

            bool sectionVisible = _wheelsSection != null && !_wheelsSection.ClassListContains("fm-wheels-section--hidden") && _wheelsGrid != null;
            bool customizeVisible = sectionVisible && _btnWheelsCustomize != null && !_btnWheelsCustomize.ClassListContains("fm-wheels-section__part--hidden");
            var labels = new System.Collections.Generic.List<string>();
            if (sectionVisible)
            {
                foreach (FMProgressWheelCard card in _wheelsGrid.Cards)
                {
                    labels.Add(card.AccessibilityLabel);
                }
            }
            string key = $"{customizeVisible}|{string.Join("\n", labels)}";

            // Rebuilding on every render would move screen-reader focus off the wheel being read.
            if (key == _wheelNodesKey)
            {
                return;
            }
            _wheelNodesKey = key;

            foreach (AccessibilityNode node in _wheelNodes)
            {
                _accessibilityHierarchy.RemoveNode(node);
            }
            _wheelNodes.Clear();
            if (_wheelsCustomizeNode != null)
            {
                _accessibilityHierarchy.RemoveNode(_wheelsCustomizeNode);
                _wheelsCustomizeNode = null;
            }

            if (sectionVisible)
            {
                for (int i = 0; i < _wheelsGrid.Cards.Count; i++)
                {
                    FMProgressWheelCard card = _wheelsGrid.Cards[i];
                    AccessibilityNode node = _accessibilityHierarchy.AddNode(labels[i]);
                    node.role = AccessibilityRole.StaticText;
                    node.frameGetter = MakeElementFrameGetter(card);
                    _wheelNodes.Add(node);
                }
            }

            if (customizeVisible)
            {
                _wheelsCustomizeNode = _accessibilityHierarchy.AddNode(ProgressWheelText.Localize("WHEELS_CUSTOMIZE"));
                _wheelsCustomizeNode.role = AccessibilityRole.Button;
                _wheelsCustomizeNode.frameGetter = MakeElementFrameGetter(_btnWheelsCustomize);
                _wheelsCustomizeNode.invoked += () =>
                {
                    OnWheelsCustomizeClicked();
                    return true;
                };
            }

            AssistiveSupport.notificationDispatcher?.SendLayoutChanged();
        }

        private static Func<Rect> MakeElementFrameGetter(VisualElement element)
        {
            return () =>
            {
                if (element == null || element.panel == null) return Rect.zero;
                var rect = element.worldBound;
                var scale = element.panel.scaledPixelsPerPoint;
                return new Rect(rect.position * scale, rect.size * scale);
            };
        }
    }
}
