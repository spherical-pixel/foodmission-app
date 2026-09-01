using System;
using System.Threading.Tasks;
using eu.foodmission.platform.Components;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
            _btOpenQuizz.style.display = DisplayStyle.None;

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
            CheckPendingNotificationPrompt();
            CheckPendingLegalConsentAsync();
        }

        private async void CheckPendingLegalConsentAsync()
        {
            if (_viewModel == null) return;
            var status = await _viewModel.CheckPendingLegalConsentAsync();
            if (status != null && status.mustAccept && status.documents != null)
            {
                foreach (var doc in status.documents)
                {
                    if (!doc.accepted)
                    {
                        bool accepted = await ShowPendingLegalConsent(doc);

                        if (!accepted)
                        {
                            break;
                        }
                    }
                }
            }
        }

        private async Task<bool> ShowPendingLegalConsent(PendingLegalConsent pendingLegalConsent)
        {
            TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();

            LegalDocument legalDocument = await _viewModel.GetLegalDocumentAsync(pendingLegalConsent.docType);

            string title = !string.IsNullOrEmpty(legalDocument?.title) ? legalDocument.title : (legalDocument.docType == LegalDocType.TermsOfService ? "@UI:T&C_TITLE" : "@UI:PRIVACY_POLICY_TITLE");
            string content = legalDocument?.content ?? "";


            NutriMessageDialog.Show(LocalizationSettings.StringDatabase.GetLocalizedString("UI", "NEW_LEGAL_DOC", new object[] { title }),
                new FMDialogAction("@UI:MENU_VIEW", () =>
                {
                    FMDialog.ShowScrollableMD(
                        this,
                        title,
                        content,
                        onAccept: async () =>
                        {
                            await AcceptPendingLegalConsent(pendingLegalConsent);
                            taskCompletionSource.TrySetResult(true);
                        }, onCancel: () =>
                        {
                            FMDialog.ShowInfo(this, "@UI:MESSAGE_TITLE_WARNING", LocalizationSettings.StringDatabase.GetLocalizedString("UI", "NOT_ACCP_LEGAL_WARNING", new object[] { title }), new FMDialogAction[]
                                {
                                    new FMDialogAction("@UI:TXT_REVIEW_DOCUMENT", async () =>
                                    {
                                        bool result = await ShowPendingLegalConsent(pendingLegalConsent);
                                        taskCompletionSource.TrySetResult(result);
                                    },ButtonVariant.Accent),
                                    new FMDialogAction("@UI:LOG_OUT", () =>
                                    {
                                        Unity.AppUI.MVVM.App.current?.services?.GetService<IStoreService>().store.Dispatch(AppActions.logout.Invoke());
                                        _navController.Navigate(Actions.go_to_auth);
                                        taskCompletionSource.TrySetResult(false);
                                    },ButtonVariant.Accent),
                                    new FMDialogAction("@UI:DELETE_ACCOUNT", () =>
                                    {
                                        DeleteAccount(() =>
                                        {
                                            taskCompletionSource.TrySetResult(false);
                                        }, async () =>
                                        {
                                            bool result = await ShowPendingLegalConsent(pendingLegalConsent);
                                            taskCompletionSource.TrySetResult(result);

                                        });

                                    },ButtonVariant.Destructive)
                                }
                            );
                        }
                    );

                }, ButtonVariant.Accent)
            );

            return await taskCompletionSource.Task;
        }

        private async Task AcceptPendingLegalConsent(PendingLegalConsent pendingLegalConsent)
        {
            await _viewModel.AcceptLegalConsentAsync(pendingLegalConsent.documentKey);
        }

        private void DeleteAccount(Action onDeleted, Action onCanceled)
        {
            FMDialog.ShowAlert(
                    App.current?.rootVisualElement,
                    "@UI:DELETE_ACCOUNT_TITLE",
                    "@UI:DELETE_ACCOUNT_MESSAGE",
                    AlertSemantic.Destructive,
                    "@UI:TXT_ACCEPT", onOk: async () =>
                    {
                        var authService = App.current?.services?.GetService<IAuthService>();
                        if (authService == null)
                        {
                            return;
                        }

                        var (success, error) = await authService.DeleteAccountAsync();
                        if (success)
                        {
                            onDeleted.Invoke();
                            var storeService = App.current?.services?.GetService<IStoreService>();
                            storeService?.store.Dispatch(AppActions.logout.Invoke());
                            _navController.Navigate(Actions.go_to_auth);
                        }
                        else
                        {
                            Debug.LogError($"[FoodmissionVisualController] Delete account failed: {error}");
                            onCanceled.Invoke();
                        }
                    },
                    "@UI:TXT_CANCEL", onKo: () =>
                    {
                        onCanceled.Invoke();
                    }
                );
        }

        private void CheckPendingNotificationPrompt()
        {
            if (_viewModel == null || !_viewModel.ShouldPromptForNotifications())
            {
                return;
            }

            NutriMessageDialog.Show(
                message: "@UI:ONBOARDING_PROFILE.NUTRI_STEP_6",
                actions: new[]
                {
                    new FMDialogAction("@UI:ONBOARDING_PROFILE.NOTIFICATIONS_OPT_YES", async () =>
                    {
                        await _viewModel.AcceptNotificationsAsync();
                    }, ButtonVariant.Accent),
                    new FMDialogAction("@UI:ONBOARDING_PROFILE.NOTIFICATIONS_OPT_NO", () =>
                    {
                        _viewModel.DeclineNotifications();
                    }, ButtonVariant.Default)
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
                        }, ButtonVariant.Accent),
                        new FMDialogAction("Más Tarde", () => { }, ButtonVariant.Default)
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
                new[] { new FMDialogAction("@UI:txtGotIt", MarkWhatsNewSeen, ButtonVariant.Accent) });
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
