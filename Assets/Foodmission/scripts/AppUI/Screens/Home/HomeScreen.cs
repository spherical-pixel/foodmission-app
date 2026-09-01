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
            CheckPendingProfileReminder();
            CheckPendingNotificationPrompt();
            CheckPendingLegalConsentAsync();
            CheckPendingPilotConsentAsync();
            CheckPendingPilotSurveyAsync();
            //SetupPilotDebugPanel();
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

        private async void CheckPendingPilotConsentAsync()
        {
            if (_viewModel == null) return;
            if (!_viewModel.IsUserInPilotCountry()) return;

            bool hasConsent = await _viewModel.HasAcceptedPilotConsentAsync();
            if (hasConsent) return;

            var (content, error) = await _viewModel.GetPilotConsentFormAsync();
            if (string.IsNullOrEmpty(content)) return;



            FMDialog.ShowScrollableMD(
                this,
                "@UI:PILOT_CONSENT_TITLE",
                content,
                onAccept: async () =>
                {
                    await _viewModel.AcceptPilotConsentAsync();
                    CheckPendingPilotSurveyAsync();
                },
                onCancel: () =>
                {
                    FMDialog.ShowInfo(
                        this,
                        "@UI:MESSAGE_TITLE_WARNING",
                        "@UI:NOT_ACCP_LEGAL_WARNING",
                        new FMDialogAction[]
                        {
                            new FMDialogAction("@UI:TXT_REVIEW_DOCUMENT", () =>
                            {
                                CheckPendingPilotConsentAsync();
                            }, ButtonVariant.Accent),
                            new FMDialogAction("@UI:TXT_CANCEL", () => { }, ButtonVariant.Default)
                        }
                    );
                }
            );


        }

        private async void CheckPendingPilotSurveyAsync()
        {
            if (_viewModel == null) return;

            var survey = await _viewModel.CheckPendingPilotSurveyAsync();
            if (survey != null)
            {


                NutriMessageDialog.Show(
                    message: "@UI:NEW_SURVEY_MESSAGE",
                    actions: new[]
                    {
                        new FMDialogAction("@UI:NEW_SURVEY_ANSWER_NOW", () =>
                        {
                            _viewModel.NavigateToPilotSurvey(survey.slug ?? survey.id);
                        }, ButtonVariant.Accent),
                        new FMDialogAction("@UI:NEW_SURVEY_LATER", () =>
                        {
                            _viewModel.PostponePilotSurvey(survey.slug);
                        }, ButtonVariant.Default),
                        new FMDialogAction("@UI:NEW_SURVEY_DECLINE", () =>
                        {
                            _viewModel.SkipPilotSurvey(survey.slug);
                        }, ButtonVariant.Default)
                    }
                );
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
                text = "🧪 Panel de Pruebas: Encuestas del Piloto",
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
                statusLabel.text = $"Ciclo: {s?.currentCycle ?? 1} | Días activos: {s?.activeDatesInCycle.Count ?? 0} | Días transcurridos: {_viewModel.GetPilotDaysSinceStart()}\n" +
                                   $"País: '{c}' (Piloto: {ip}) | Bypass Elegibilidad: {(bypass ? "SÍ" : "NO")}\n" +
                                   $"Completadas: [{(s?.completedSlugsInCycle != null && s.completedSlugsInCycle.Count > 0 ? string.Join(", ", s.completedSlugsInCycle) : "Ninguna")}]";
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
                title = _viewModel.DebugBypassEligibility ? "Bypass: ACTIVO" : "Activar Bypass",
                size = Size.S,
                variant = _viewModel.DebugBypassEligibility ? ButtonVariant.Accent : ButtonVariant.Default
            };
            btnBypass.clicked += () =>
            {
                _viewModel.DebugBypassEligibility = !_viewModel.DebugBypassEligibility;
                btnBypass.title = _viewModel.DebugBypassEligibility ? "Bypass: ACTIVO" : "Activar Bypass";
                btnBypass.variant = _viewModel.DebugBypassEligibility ? ButtonVariant.Accent : ButtonVariant.Default;
                refreshStatus();
            };
            helpersRow.Add(btnBypass);

            var btnSimulateDE = new FMButton
            {
                title = "Fijar País 'DE' + Consent.",
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

            var stepperLabel = new Unity.AppUI.UI.Text { text = "Simular Día: ", size = TextSize.S };
            stepperLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            stepperRow.Add(stepperLabel);

            var choices = new string[]
            {
                "Día 1 (sin encuesta)",
                "Día 2 (second-use)",
                "Día 3 (third-use)",
                "Día 4 (fourth-use)",
                "Día 5 (fifth-use)",
                "Día 6 (sixth-use)",
                "Día 7 (seventh)",
                "Día 8 + 30d (after-1-mt...)",
                "Día 9 + 30d (after-1-m...)",
                "Día 10 + 30d (after-1-m...)",
                "Día 11 + 30d (end)"
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
                title = "Evaluar Encuesta",
                size = Size.S,
                variant = ButtonVariant.Accent
            };
            btnCheck.clicked += () =>
            {
                CheckPendingPilotSurveyAsync();
            };
            btnRow.Add(btnCheck);

            var btnResetSurveys = new FMButton
            {
                title = "Reset Encuestas Ciclo",
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
