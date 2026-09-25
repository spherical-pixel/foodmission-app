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
        private static bool _hasDeferredOnboardingThisSession = false;
        private static bool _hasDeferredNotificationsThisSession = false;
        private static bool _hasDeferredPilotSurveyThisSession = false;

        public static void ResetSessionDeferredFlags()
        {
            _hasDeferredOnboardingThisSession = false;
            _hasDeferredNotificationsThisSession = false;
            _hasDeferredPilotSurveyThisSession = false;
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
        private bool _isDisplayingCelebrationQueue;

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

            EvaluateNextPendingHomePromptAsync();
        }

        private async void EvaluateNextPendingHomePromptAsync()
        {
            if (_viewModel == null) return;

            // 1. Consentimiento legal obligatorio (máxima prioridad)
            if (await CheckPendingLegalConsentAsync()) return;

            // 2. Consentimiento piloto obligatorio
            if (await CheckPendingPilotConsentAsync()) return;

            // 3. Recompensas de gamificación (si hay celebración con avatar/partículas, no abrir más ventanas)
            if (await CheckPendingGamificationRewardsAsync()) return;

            // 4. Novedades de la versión (What's New)
            if (await CheckWhatsNewAsync()) return;

            // 5. Recordatorio de Onboarding (perfil o encuesta inicial)
            if (CheckPendingProfileReminder()) return;

            // 6. Solicitud de notificaciones push
            if (CheckPendingNotificationPrompt()) return;

            // 7. Encuesta periódica del piloto
            if (await CheckPendingPilotSurveyAsync()) return;
        }

        private async Task<bool> CheckPendingLegalConsentAsync()
        {
            if (_viewModel == null) return false;
            var status = await _viewModel.CheckPendingLegalConsentAsync();
            if (status != null && status.mustAccept && status.documents != null)
            {
                foreach (var doc in status.documents)
                {
                    if (!doc.accepted)
                    {
                        _ = ShowPendingLegalConsent(doc);
                        return true;
                    }
                }
            }
            return false;
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

        private bool CheckPendingNotificationPrompt()
        {
            if (_viewModel == null || _hasDeferredNotificationsThisSession || !_viewModel.ShouldPromptForNotifications())
            {
                return false;
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
                        _hasDeferredNotificationsThisSession = true;
                        _viewModel.DeclineNotifications();
                    }, ButtonVariant.Default)
                }
            );

            return true;
        }

        private async Task<bool> CheckPendingPilotConsentAsync()
        {
            if (_viewModel == null) return false;
            if (!_viewModel.IsUserInPilotCountry()) return false;

            bool hasConsent = await _viewModel.HasAcceptedPilotConsentAsync();
            if (hasConsent) return false;

            var (content, error) = await _viewModel.GetPilotConsentFormAsync();
            if (string.IsNullOrEmpty(content)) return false;

            FMDialog.ShowScrollableMD(
                this,
                "@UI:PILOT_CONSENT_TITLE",
                content,
                onAccept: async () =>
                {
                    await _viewModel.AcceptPilotConsentAsync();
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
                                _ = CheckPendingPilotConsentAsync();
                            }, ButtonVariant.Accent),
                            new FMDialogAction("@UI:TXT_CANCEL", () => { }, ButtonVariant.Default)
                        }
                    );
                }
            );

            return true;
        }

        private async Task<bool> CheckPendingPilotSurveyAsync()
        {
            if (_viewModel == null || _hasDeferredPilotSurveyThisSession) return false;

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
                            _hasDeferredPilotSurveyThisSession = true;
                            _viewModel.PostponePilotSurvey(survey.slug);
                        }, ButtonVariant.Default),
                        new FMDialogAction("@UI:NEW_SURVEY_DECLINE", () =>
                        {
                            _hasDeferredPilotSurveyThisSession = true;
                            _viewModel.SkipPilotSurvey(survey.slug);
                        }, ButtonVariant.Default)
                    }
                );

                return true;
            }

            return false;
        }

        private async Task<bool> CheckPendingGamificationRewardsAsync()
        {
            if (_viewModel == null || _isDisplayingCelebrationQueue) return false;

            var rewards = await _viewModel.CheckPendingGamificationRewardsAsync();
            if (rewards != null && rewards.Count > 0)
            {
                ShowCelebrationQueue(new System.Collections.Generic.Queue<PendingRewardCelebration>(rewards));
                return true;
            }

            return false;
        }

        private void ShowCelebrationQueue(System.Collections.Generic.Queue<PendingRewardCelebration> queue)
        {
            if (queue == null || queue.Count == 0)
            {
                _isDisplayingCelebrationQueue = false;
                _ = _viewModel?.LoadActiveQuestAsync();
                RefreshActiveQuestWidget();
                return;
            }

            _isDisplayingCelebrationQueue = true;
            var item = queue.Dequeue();

            RewardCelebrationDialog.Show(
                item.Reward,
                contextTitle: item.ContextTitle,
                onDismiss: () =>
                {
                    if (queue.Count > 0)
                    {
                        schedule.Execute(() => ShowCelebrationQueue(queue)).StartingIn(250);
                    }
                    else
                    {
                        _isDisplayingCelebrationQueue = false;
                        _ = _viewModel?.LoadActiveQuestAsync();
                        RefreshActiveQuestWidget();
                    }
                }
            );
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

                RewardCelebrationDialog.Show(
                    simulatedReward
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
                CheckPendingPilotSurveyAsync();
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

        private bool CheckPendingProfileReminder()
        {
            if (_viewModel == null || _hasDeferredOnboardingThisSession) return false;

            var pendingType = _viewModel.GetPendingOnboardingType();
            if (pendingType == PendingOnboardingType.None) return false;

            string messageKey = pendingType switch
            {
                PendingOnboardingType.Profile => "ONBOARDING_REMINDER_PROFILE_MSG",
                PendingOnboardingType.Survey  => "ONBOARDING_REMINDER_SURVEY_MSG",
                PendingOnboardingType.Goals   => "ONBOARDING_REMINDER_GOALS_MSG",
                _                             => "ONBOARDING_REMINDER_PROFILE_MSG"
            };

            string actionKey = pendingType switch
            {
                PendingOnboardingType.Profile => "ONBOARDING_REMINDER_BTN_COMPLETE_PROFILE",
                PendingOnboardingType.Survey  => "ONBOARDING_REMINDER_BTN_COMPLETE_SURVEY",
                PendingOnboardingType.Goals   => "ONBOARDING_REMINDER_BTN_COMPLETE_GOALS",
                _                             => "ONBOARDING_REMINDER_BTN_COMPLETE_PROFILE"
            };

            NutriMessageDialog.Show(
                message: LocalizationSettings.StringDatabase.GetLocalizedString("UI", messageKey),
                actions: new[]
                {
                    new FMDialogAction(
                        LocalizationSettings.StringDatabase.GetLocalizedString("UI", actionKey),
                        () =>
                        {
                            if (pendingType == PendingOnboardingType.Profile)
                            {
                                _viewModel.NavigateToOnboardingProfile();
                            }
                            else if (pendingType == PendingOnboardingType.Survey)
                            {
                                _viewModel.NavigateToOnboardingSurvey();
                            }
                            else
                            {
                                _viewModel.NavigateToOnboardingGoals();
                            }
                        },
                        ButtonVariant.Accent
                    ),
                    new FMDialogAction(
                        LocalizationSettings.StringDatabase.GetLocalizedString("UI", "LATER"),
                        () =>
                        {
                            _hasDeferredOnboardingThisSession = true;
                        },
                        ButtonVariant.Default
                    )
                }
            );

            return true;
        }

        private void RegisterEvents()
        {
            if (_activeQuestCard != null)
            {
                _activeQuestCard.Clicked += OnActiveQuestClicked;
                _activeQuestCard.QuickMealClicked += OnActiveQuestQuickMealClicked;
            }
            if (_btnChooseQuest != null) _btnChooseQuest.clicked += OnChooseQuestClicked;
            if (_viewModel != null) _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        private void UnregisterEvents()
        {
            if (_activeQuestCard != null)
            {
                _activeQuestCard.Clicked -= OnActiveQuestClicked;
                _activeQuestCard.QuickMealClicked -= OnActiveQuestQuickMealClicked;
            }
            if (_btnChooseQuest != null) _btnChooseQuest.clicked -= OnChooseQuestClicked;
            if (_viewModel != null) _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        private void OnActiveQuestClicked()
        {
            _viewModel?.OpenCurrentQuest();
        }

        private void OnActiveQuestQuickMealClicked()
        {
            _viewModel?.NavigateToQuickMealLog();
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

        private async Task<bool> CheckWhatsNewAsync()
        {
            try
            {
                var whatsNewService = App.current?.services?.GetService<IWhatsNewService>();
                if (whatsNewService == null) return false;

                var (shouldShow, notes) = await whatsNewService.CheckShouldShowAsync();
                if (shouldShow)
                {
                    ShowWhatsNewModal(notes);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HomeScreen] What's New check failed: {ex.Message}");
            }
            return false;
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


            _activeQuestCard = null;
            _noActiveQuestBanner = null;
            _btnChooseQuest = null;

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
