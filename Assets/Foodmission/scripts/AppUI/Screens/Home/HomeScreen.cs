using System;
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

        private FMButton _btOpenQuizz;

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
        }

        private void CacheUIElements()
        {
            _healthProgress = contentContainer.Q<LinearProgress>("health-progress");
            _sustainabilityProgress = contentContainer.Q<LinearProgress>("sustainability-progress");
            _knowledgeProgress = contentContainer.Q<LinearProgress>("knowledge-progress");
            _caloriesCircular = contentContainer.Q<CircularProgress>("calories-circular");
            _caloriesConsumedLabel = contentContainer.Q<Label>("calories-consumed");
            _caloriesLeftLabel = contentContainer.Q<Label>("calories-left");

            _btOpenQuizz = contentContainer.Q<FMButton>("open-quiz");

            _periodStepper = contentContainer.Q<FMArrowStepper>("period-stepper");
            _scopeStepper = contentContainer.Q<FMArrowStepper>("scope-stepper");
        }

        protected override void OnViewModelBound()
        {
            base.OnViewModelBound();
            RegisterEvents();
            RefreshStats();
            SetupSteppers();

            CheckWhatsNewAsync();
            //CheckPendingProfileReminder();
            //CheckPendingNotificationPrompt();
        }

        private void CheckPendingNotificationPrompt()
        {
            if (_viewModel == null || !_viewModel.ShouldPromptForNotifications())
            {
                return;
            }

            NutriMessageDialog.Show(
                message: "¡Mantente al día con Foodmission! 🔔\n\n¿Quieres activar las notificaciones para recibir recordatorios de tus comidas y avisos cuando los alimentos de tu despensa estén a punto de caducar?",
                actions: new[]
                {
                    new FMDialogAction("Activar", async () =>
                    {
                        await _viewModel.AcceptNotificationsAsync();
                    }, isPrimary: true),
                    new FMDialogAction("Ahora no", () =>
                    {
                        _viewModel.DeclineNotifications();
                    }, isPrimary: false)
                }
            );
        }

        private void CheckPendingProfileReminder()
        {
            var storeService = App.current?.services?.GetService<IStoreService>();
            if (storeService == null) return;

            var state = storeService.GetAppState();
            if (!state.hasCompletedExtendedProfile && state.hasSkippedExtendedProfile)
            {
                NutriMessageDialog.Show(
                    message: "Tienes pendiente completar tu perfil extendido. ¿Quieres completarlo ahora?",
                    actions: new[]
                    {
                        new FMDialogAction("Completar Perfil", () =>
                        {
                            _viewModel?.NavigateToOnboardingProfile();
                        }, isPrimary: true),
                        new FMDialogAction("Más Tarde", () => { }, isPrimary: false)
                    }
                );
            }
        }

        private void RegisterEvents()
        {
            _btOpenQuizz.clicked += OnOpenQuizOpen;
        }
        private void UnregisterEvents()
        {
            _btOpenQuizz.clicked -= OnOpenQuizOpen;
        }

        private void OnOpenQuizOpen()
        {
            Debug.Log("OnOpenQuizOpen");
            // Q1.1.1
            // df27b23d-ea27-4c7e-93f5-26e3307fefdf
            _navController.Navigate(Actions.open_quiz, new[] { new Argument("code", "Q1.1.1"), new Argument("id", "df27b23d-ea27-4c7e-93f5-26e3307fefdf") });
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

        private async void CheckWhatsNewAsync()
        {
            try
            {
                var whatsNewService = App.current?.services?.GetService<IWhatsNewService>();
                if (whatsNewService == null) return;

                var (shouldShow, notes) = await whatsNewService.CheckShouldShowAsync();
                if (shouldShow)
                {
                    ShowWhatsNewModal(notes);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HomeScreen] What's New check failed: {ex.Message}");
            }
        }

        private void ShowWhatsNewModal(string releaseNotes)
        {
            FMDialog.ShowInfo(
                contentContainer,
                LocalizationSettings.StringDatabase.GetLocalizedString("UI", "txtWhatsNew", new object[] { Application.version }),
                releaseNotes ?? "No release notes available.",
                new[] { new FMDialogAction("@UI:txtGotIt", MarkWhatsNewSeen, isPrimary: true) });
        }

        private async void MarkWhatsNewSeen()
        {
            try
            {
                var whatsNewService = App.current?.services?.GetService<IWhatsNewService>();
                if (whatsNewService != null)
                    await whatsNewService.MarkAsSeenAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HomeScreen] Failed to mark What's New as seen: {ex.Message}");
            }
        }

        protected override void OnViewModelUnbinding()
        {
            UnregisterEvents();

            _healthProgress = null;
            _sustainabilityProgress = null;
            _knowledgeProgress = null;
            _caloriesCircular = null;
            _caloriesConsumedLabel = null;
            _caloriesLeftLabel = null;
            _periodStepper = null;
            _scopeStepper = null;

            _btOpenQuizz = null;

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
        }

        protected override void TeardownAccessibilityNodes()
        {
            _healthProgressNode = null;
            _sustainabilityProgressNode = null;
            _knowledgeProgressNode = null;
            _caloriesNode = null;

            _periodStepper?.DestroyAccessibilityNode();
            _scopeStepper?.DestroyAccessibilityNode();

            base.TeardownAccessibilityNodes();
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
