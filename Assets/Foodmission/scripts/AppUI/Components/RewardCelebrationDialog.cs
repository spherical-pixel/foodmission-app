using System;
using System.Collections.Generic;
using System.Linq;
using eu.foodmission.platform.Utils;
using MainraGames;
using Unity.AppUI.Core;
using Unity.AppUI.MVVM;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;

namespace eu.foodmission.platform.Components
{
    public enum RewardType
    {
        Xp,
        Points,
        Badge,
        AvatarItem,
        PetItem,
        Collectible
    }

    public class RewardPresentationItem
    {
        public RewardType Type { get; set; }
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public string IconEmoji { get; set; }
        public int Value { get; set; }
        public string RawId { get; set; }
    }

    /// <summary>
    /// Global full-screen celebratory modal displayed when a user earns gamification rewards
    /// (XP, Points, Badges, Avatar items). Features the 3D Avatar in FullBody celebration mode,
    /// particle effects, and animated sequential presentation cards with tap-to-advance.
    /// </summary>
    public static class RewardCelebrationDialog
    {
        private static bool s_IsAdvancing;
        private static Modal s_CurrentModal;
        private static VisualTreeAsset s_CachedConfetiParticlesTemplate;

        private static void LoadParticlesTemplateAsync(Action onComplete)
        {
            if (s_CachedConfetiParticlesTemplate != null)
            {
                onComplete?.Invoke();
                return;
            }

            Addressables.LoadAssetAsync<VisualTreeAsset>("ui-template/particles/confeti").Completed += handle =>
            {
                if (handle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
                {
                    s_CachedConfetiParticlesTemplate = handle.Result;
                }
                else
                {
                    Debug.LogWarning($"[RewardCelebrationDialog] Failed to load Addressable ui-template/particles/confeti: {handle.OperationException}");
                }
                onComplete?.Invoke();
            };
        }

        /// <summary>
        /// Builds the presentation queue of individual reward items from a ContentReward payload.
        /// </summary>
        public static List<RewardPresentationItem> BuildPresentationQueue(ContentReward reward, string contextTitle = null)
        {
            var list = new List<RewardPresentationItem>();
            if (reward == null)
            {
                return list;
            }

            if (reward.xp.HasValue && reward.xp.Value > 0)
            {
                list.Add(new RewardPresentationItem
                {
                    Type = RewardType.Xp,
                    Title = $"+{reward.xp.Value} XP",
                    Subtitle = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "REWARD_XP_EARNED", new object[] { reward.xp.Value }),
                    Value = reward.xp.Value
                });
            }

            if (reward.points.HasValue && reward.points.Value > 0)
            {
                list.Add(new RewardPresentationItem
                {
                    Type = RewardType.Points,
                    Title = $"+{reward.points.Value}",
                    Subtitle = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "REWARD_POINTS_EARNED", new object[] { reward.points.Value }),
                    Value = reward.points.Value
                });
            }

            if (!string.IsNullOrEmpty(reward.badgeId))
            {
                list.Add(new RewardPresentationItem
                {
                    Type = RewardType.Badge,
                    Title = "@UI:REWARD_BADGE_UNLOCKED",
                    Subtitle = reward.badgeId,
                    IconEmoji = "🏅",
                    Value = 1,
                    RawId = reward.badgeId
                });
            }

            if (!string.IsNullOrEmpty(reward.avatarItem))
            {
                list.Add(new RewardPresentationItem
                {
                    Type = RewardType.AvatarItem,
                    Title = "@UI:REWARD_AVATAR_ITEM",
                    Subtitle = reward.avatarItem,
                    IconEmoji = "🎁",
                    Value = 1,
                    RawId = reward.avatarItem
                });
            }

            if (!string.IsNullOrEmpty(reward.petItem))
            {
                list.Add(new RewardPresentationItem
                {
                    Type = RewardType.PetItem,
                    Title = "@UI:REWARD_PET_ITEM",
                    Subtitle = reward.petItem,
                    IconEmoji = "🐾",
                    Value = 1,
                    RawId = reward.petItem
                });
            }

            if (!string.IsNullOrEmpty(reward.collectible))
            {
                list.Add(new RewardPresentationItem
                {
                    Type = RewardType.Collectible,
                    Title = "@UI:REWARD_COLLECTIBLE",
                    Subtitle = reward.collectible,
                    IconEmoji = "🏆",
                    Value = 1,
                    RawId = reward.collectible
                });
            }

            return list;
        }

        /// <summary>
        /// Displays the global reward celebration modal for the earned rewards.
        /// </summary>
        public static void Show(ContentReward reward, string contextTitle = null, Action onDismiss = null)
        {
            if (reward == null)
            {
                onDismiss?.Invoke();
                return;
            }

            var queue = BuildPresentationQueue(reward, contextTitle);
            if (queue.Count == 0)
            {
                onDismiss?.Invoke();
                return;
            }

            var rootElement = App.current?.rootVisualElement;
            if (rootElement == null)
            {
                Debug.LogWarning("[RewardCelebrationDialog] Cannot show celebration modal: App.current or rootVisualElement is null.");
                onDismiss?.Invoke();
                return;
            }


            LoadParticlesTemplateAsync(() =>
            {
                ShowModal(rootElement, queue, contextTitle, onDismiss);
            });

        }


        private static void ShowModal(
            VisualElement panelRoot,
            List<RewardPresentationItem> queue,
            string contextTitle,
            Action onDismiss)
        {
            // Close any existing celebration modal first
            if (s_CurrentModal != null)
            {
                s_CurrentModal.Dismiss(DismissType.Action);
                s_CurrentModal = null;
            }

            s_IsAdvancing = false;

            // Root Modal Container
            var root = new VisualElement();
            root.AddToClassList("fm-reward-modal-root");

            var rootContainer = new VisualElement();
            rootContainer.AddToClassList("fm-reward-modal-root-container");

            // Avatar & Stage Background Area
            var avatarStage = new VisualElement();
            avatarStage.AddToClassList("fm-reward-avatar-stage");
            avatarStage.pickingMode = PickingMode.Ignore;


            if (s_CachedConfetiParticlesTemplate != null)
            {
                VisualElement particlesConfeti = new VisualElement();
                s_CachedConfetiParticlesTemplate.CloneTree(particlesConfeti);
                particlesConfeti.style.position = Position.Absolute;
                particlesConfeti.style.top = Length.Percent(0);
                particlesConfeti.style.left = Length.Percent(0);
                particlesConfeti.style.right = Length.Percent(0);
                particlesConfeti.style.bottom = Length.Percent(0);

                avatarStage.Add(particlesConfeti);
            }


            // Spotlight Image
            var spotlight = new VisualElement();
            spotlight.AddToClassList("fm-reward-spotlight");
            avatarStage.Add(spotlight);

            // FullBody Avatar
            var avatarView = new FMAvatarView { Mode = AvatarViewMode.FullBody };
            avatarView.style.flexGrow = 1;
            avatarView.style.alignSelf = Align.Center;
            avatarView.style.width = Length.Percent(100);
            avatarView.style.height = Length.Percent(100);
            avatarView.style.marginBottom = Length.Pixels(100);
            avatarStage.Add(avatarView);


            root.Add(avatarStage);
            root.Add(rootContainer);

            // Header Container & Title
            var headerContainer = new VisualElement();
            headerContainer.AddToClassList("fm-reward-header-container");
            var headerTitle = new Text();
            headerTitle.size = TextSize.XXXL;
            headerTitle.AddToClassList("fm-reward-celebration-title");
            headerTitle.text = !string.IsNullOrEmpty(contextTitle) ? contextTitle : "@UI:REWARD_CELEBRATION_TITLE";
            headerContainer.Add(headerTitle);
            rootContainer.Add(headerContainer);

            // Center Reward Card Container
            var cardContainer = new VisualElement();
            cardContainer.AddToClassList("fm-reward-card-container");

            var rewardCard = new ExVisualElement();

            rewardCard.AddToClassList("fm-reward-card");

            cardContainer.Add(rewardCard);
            rootContainer.Add(cardContainer);

            // Bottom Continue Button
            var bottomContainer = new VisualElement();
            bottomContainer.AddToClassList("fm-reward-bottom-container");

            var btnContinue = new FMButton
            {
                title = "@UI:TXT_CONTINUE",
                trailingIcon = "fm-arrow-right",
                variant = ButtonVariant.Accent,
                size = Size.L
            };
            btnContinue.AddToClassList("fm-button");
            btnContinue.AddToClassList("fm-button-align-left");
            btnContinue.style.width = Length.Percent(100);
            bottomContainer.Add(btnContinue);
            rootContainer.Add(bottomContainer);

            // Apply Safe Area
            App.current?.services?.GetService<IThemeService>()?.ApplySafeAreaPadding(rootContainer, true, true, false, false);



            // Audio & Mascot Feedback
            var audioService = App.current?.services?.GetService<IAudioService>();
            var sfx = queue.Any(q => q.Type == RewardType.Badge) ? SfxType.WinBadge : SfxType.MissionCompleted;
            audioService?.PlaySfx(sfx);

            var avatarService = App.current?.services?.GetService<IAvatarService>();
            if (avatarService?.AvatarController?.AvatarAnimationController != null)
            {
                avatarService.AvatarController.AvatarAnimationController.CurrentMood = AvatarMood.Happy;
                avatarService.AvatarController.AvatarAnimationController.TriggerCelebration();
            }


            // Build Modal
            var modal = Modal.Build(panelRoot, root);
            modal.SetFullScreenMode(ModalFullScreenMode.FullScreenTakeOver);
            s_CurrentModal = modal;

            int currentIndex = 0;
            bool isDismissed = false;

            void DismissAndCleanup()
            {
                if (isDismissed) return;
                isDismissed = true;
                s_IsAdvancing = false;

                if (modal != null)
                {
                    modal.Dismiss(DismissType.Action);
                    s_CurrentModal = null;
                }

                onDismiss?.Invoke();
            }

            void DisplayItem(int index)
            {
                if (index < 0 || index >= queue.Count)
                {
                    DismissAndCleanup();
                    return;
                }

                var item = queue[index];

                // Dynamically generate visual content using the dedicated builder for this reward type
                rewardCard.Clear();
                var visualContent = CreateRewardVisual(item);
                if (visualContent != null)
                {
                    rewardCard.Add(visualContent);
                }



                // Update Button Text & Icon on last item
                if (index == queue.Count - 1)
                {
                    btnContinue.title = "@UI:TXT_DONE";
                    btnContinue.trailingIcon = "fm-check";
                }
                else
                {
                    btnContinue.title = "@UI:TXT_CONTINUE";
                    btnContinue.trailingIcon = "fm-arrow-right";
                }

                // Audio feedback for subsequent steps
                if (index > 0)
                {
                    var stepSfx = item.Type == RewardType.Badge ? SfxType.WinBadge : SfxType.MissionCompleted;
                    audioService?.PlaySfx(stepSfx);
                }

                var avatarService = App.current?.services?.GetService<IAvatarService>();
                if (avatarService?.AvatarController?.AvatarAnimationController != null)
                {
                    avatarService.AvatarController.AvatarAnimationController.TriggerCelebration();
                }

                // Slide In
                rewardCard.RemoveFromClassList("fm-reward-card--exit");
                rewardCard.AddToClassList("fm-reward-card--visible");
            }

            void Advance()
            {
                if (isDismissed || s_IsAdvancing) return;
                s_IsAdvancing = true;
                btnContinue.SetEnabled(false);

                // If on last item, dismiss immediately
                if (currentIndex >= queue.Count - 1)
                {
                    DismissAndCleanup();
                    return;
                }

                // Animate Slide Out
                rewardCard.RemoveFromClassList("fm-reward-card--visible");
                rewardCard.AddToClassList("fm-reward-card--exit");

                root.schedule.Execute(() =>
                {
                    currentIndex++;
                    s_IsAdvancing = false;
                    btnContinue.SetEnabled(true);
                    DisplayItem(currentIndex);
                }).StartingIn(220);
            }

            // Click Handler: Step-by-step navigation strictly driven by button
            btnContinue.clicked += () => Advance();

            modal.dismissed += (_, _) =>
            {
                if (!isDismissed)
                {
                    DismissAndCleanup();
                }
            };

            modal.Show();

            // Initial presentation
            root.schedule.Execute(() => DisplayItem(0)).StartingIn(50);
        }

        // ── Reward Visual Builders ──────────────────────────────────────────

        /// <summary>
        /// Main factory that routes to the specific visual builder based on the reward type.
        /// </summary>
        public static VisualElement CreateRewardVisual(RewardPresentationItem item)
        {
            if (item == null) return new VisualElement();

            return item.Type switch
            {
                RewardType.Xp => BuildXpContent(item),
                RewardType.Points => BuildPointsContent(item),
                RewardType.Badge => BuildBadgeContent(item),
                RewardType.AvatarItem => BuildAvatarItemContent(item),
                RewardType.PetItem => BuildPetItemContent(item),
                RewardType.Collectible => BuildCollectibleContent(item),
                _ => BuildDefaultContent(item)
            };
        }

        /// <summary>
        /// Visual content builder for XP rewards.
        /// Customize this function to alter how XP rewards are displayed.
        /// </summary>
        public static VisualElement BuildXpContent(RewardPresentationItem item)
        {
            var container = CreateContentContainer("fm-reward-content--xp");
            IStoreService storeService = App.current?.services?.GetService<IStoreService>();

            int userXp = 0;

            if (storeService != null)
            {
                //userXp = storeService.GetAppState().userXp;
                // Let's fake for testing 
                userXp = 10 + item.Value;
            }

            int prevXp = userXp - item.Value;
            var startInfo = LevelFormula.GetProgressInfo(prevXp);

            var xpRow = new VisualElement();
            xpRow.style.flexDirection = FlexDirection.Row;
            xpRow.style.alignItems = Align.Center;
            xpRow.style.width = Length.Percent(80);

            var xpBar = new LinearProgress();
            xpBar.value = startInfo.NormalizedProgress;
            xpBar.AddToClassList("fm-xp-progress");
            xpBar.AddToClassList("appui-progress--rounded-corners");
            xpBar.style.flexGrow = 1;
            xpBar.variant = Progress.Variant.Determinate;

            var xpBadge = new VisualElement();
            xpBadge.AddToClassList("fm-profile-xp-badge");

            var xpLabel = new Label();
            xpLabel.text = startInfo.Level.ToString();
            xpLabel.AddToClassList("fm-profile-xp-label");
            xpBadge.Add(xpLabel);

            xpRow.Add(xpBar);
            xpRow.Add(xpBadge);

            container.Add(xpRow);

            var title = new Text { text = item?.Title ?? "" };
            title.size = TextSize.XXXL;
            title.AddToClassList("fm-reward-title");
            container.Add(title);

            var subtitle = new Text { text = item?.Subtitle ?? "" };
            subtitle.size = TextSize.XXXL;
            subtitle.AddToClassList("fm-reward-subtitle");
            container.Add(subtitle);

            // Animate XP bar progression sequentially across levels
            bool animationStarted = false;
            void StartAnimation()
            {
                if (animationStarted) return;
                animationStarted = true;
                AnimateXpProgression(container, xpBar, xpLabel, xpBadge, prevXp, userXp);
            }

            if (container.panel != null)
            {
                StartAnimation();
            }
            else
            {
                container.RegisterCallback<AttachToPanelEvent>(_ => StartAnimation());
            }

            return container;
        }

        /// <summary>
        /// Animates the LinearProgress XP bar and level badge sequentially through all stages
        /// (including intermediate level ups and final fractional progress) using UI Toolkit's scheduler.
        /// </summary>
        public static IVisualElementScheduledItem AnimateXpProgression(
            VisualElement host,
            LinearProgress xpBar,
            Label xpLabel,
            VisualElement xpBadge,
            int startXp,
            int endXp,
            Action onComplete = null)
        {
            if (host == null || xpBar == null)
            {
                onComplete?.Invoke();
                return null;
            }

            var steps = LevelFormula.CalculateAnimationSteps(startXp, endXp);
            if (steps == null || steps.Count == 0)
            {
                onComplete?.Invoke();
                return null;
            }

            // Set initial state
            xpBar.value = steps[0].StartProgress;
            if (xpLabel != null)
            {
                xpLabel.text = steps[0].Level.ToString();
            }

            int currentStepIndex = 0;
            float stateTime = 0f;
            bool isPausingOnLevelUp = false;
            float initialDelay = 0.25f; // Wait 250ms for the modal card slide-in transition to settle
            bool isDelaying = true;

            IVisualElementScheduledItem scheduledItem = null;

            scheduledItem = host.schedule.Execute(timerState =>
            {
                // Guard: if host is detached or modal is dismissed, halt execution
                if (host.panel == null)
                {
                    scheduledItem?.Pause();
                    return;
                }

                float dt = timerState.deltaTime / 1000f;
                if (dt > 0.1f) dt = 0.1f;

                if (isDelaying)
                {
                    stateTime += dt;
                    if (stateTime >= initialDelay)
                    {
                        isDelaying = false;
                        stateTime = 0f;
                    }
                    return;
                }

                if (isPausingOnLevelUp)
                {
                    stateTime += dt;
                    if (stateTime >= 0.25f) // 250ms pause after level up
                    {
                        isPausingOnLevelUp = false;
                        stateTime = 0f;

                        currentStepIndex++;
                        if (currentStepIndex < steps.Count)
                        {
                            xpBar.value = steps[currentStepIndex].StartProgress;
                        }
                        else
                        {
                            xpBar.value = 0f;
                            scheduledItem?.Pause();
                            onComplete?.Invoke();
                        }
                    }
                    return;
                }

                if (currentStepIndex >= steps.Count)
                {
                    scheduledItem?.Pause();
                    onComplete?.Invoke();
                    return;
                }

                var currentStep = steps[currentStepIndex];
                float progressDistance = Mathf.Abs(currentStep.EndProgress - currentStep.StartProgress);
                // Proportional duration: between 350ms and 600ms depending on distance filled
                float stepDuration = Mathf.Clamp(progressDistance * 0.7f, 0.35f, 0.6f);

                stateTime += dt;
                float t = Mathf.Clamp01(stateTime / stepDuration);

                // Easing:
                // If this step hits 100% (level up), use EaseInQuad for momentum towards 100%
                // If it's the final stop, use EaseOutQuad for a smooth deceleration
                float easedT = currentStep.IsLevelUp ? (t * t) : (t * (2f - t));
                xpBar.value = Mathf.Lerp(currentStep.StartProgress, currentStep.EndProgress, easedT);

                if (t >= 1f)
                {
                    xpBar.value = currentStep.EndProgress;

                    if (currentStep.IsLevelUp)
                    {
                        // Level up!
                        int nextLevel = currentStep.Level + 1;
                        if (xpLabel != null)
                        {
                            xpLabel.text = nextLevel.ToString();
                        }

                        // Punch scale animation on badge
                        if (xpBadge != null)
                        {
                            xpBadge.style.scale = new StyleScale(new Scale(new Vector3(1.25f, 1.25f, 1f)));
                            xpBadge.schedule.Execute(() =>
                            {
                                if (xpBadge.panel != null)
                                {
                                    xpBadge.style.scale = new StyleScale(new Scale(Vector3.one));
                                }
                            }).StartingIn(180);
                        }

                        // Audio & mascot celebration
                        var audioService = App.current?.services?.GetService<IAudioService>();
                        audioService?.PlaySfx(SfxType.WinBadge);

                        var avatarService = App.current?.services?.GetService<IAvatarService>();
                        if (avatarService?.AvatarController?.AvatarAnimationController != null)
                        {
                            avatarService.AvatarController.AvatarAnimationController.CurrentMood = AvatarMood.Happy;
                            avatarService.AvatarController.AvatarAnimationController.TriggerCelebration();
                        }

                        // Start level up pause
                        isPausingOnLevelUp = true;
                        stateTime = 0f;
                    }
                    else
                    {
                        // Final step reached
                        scheduledItem?.Pause();
                        onComplete?.Invoke();
                    }
                }
            }).Every(16);

            return scheduledItem;
        }

        /// <summary>
        /// Visual content builder for Points / Currency rewards.
        /// Customize this function to alter how Points rewards are displayed.
        /// </summary>
        public static VisualElement BuildPointsContent(RewardPresentationItem item)
        {
            var container = CreateContentContainer("fm-reward-content--points");

            Image imageCoin = new Image();

            App.current?.services?.GetService<ISpriteService>()?.BindSprite(imageCoin, "sprites/coin-big");
            imageCoin.style.width = Length.Pixels(456); //456
            imageCoin.style.height = Length.Pixels(456);
            imageCoin.style.alignContent = Align.Center;
            imageCoin.style.justifyContent = Justify.Center;
            container.Add(imageCoin);

            var title = new Text { text = item?.Title ?? "" };
            title.AddToClassList("fm-reward-title");
            imageCoin.Add(title);

            var subtitle = new Text { text = item?.Subtitle ?? "" };
            subtitle.AddToClassList("fm-reward-subtitle");
            container.Add(subtitle);

            return container;
        }

        /// <summary>
        /// Visual content builder for Badge rewards.
        /// Customize this function to alter how Badge rewards are displayed.
        /// </summary>
        public static VisualElement BuildBadgeContent(RewardPresentationItem item)
        {
            var container = CreateContentContainer("fm-reward-content--badge");

            var iconCircle = new VisualElement();
            iconCircle.AddToClassList("fm-reward-icon-circle");
            var iconText = new Text { text = item?.IconEmoji ?? "🏅" };
            iconText.AddToClassList("fm-reward-icon-emoji");
            iconCircle.Add(iconText);
            container.Add(iconCircle);

            var title = new Text { text = item?.Title ?? "" };
            title.AddToClassList("fm-reward-title");
            container.Add(title);

            var subtitle = new Text { text = item?.Subtitle ?? "" };
            subtitle.AddToClassList("fm-reward-subtitle");
            container.Add(subtitle);

            return container;
        }

        /// <summary>
        /// Visual content builder for Avatar Item (cosmetics/accessories) rewards.
        /// Customize this function to alter how Avatar Item rewards are displayed.
        /// </summary>
        public static VisualElement BuildAvatarItemContent(RewardPresentationItem item)
        {
            var container = CreateContentContainer("fm-reward-content--avatar-item");

            var iconCircle = new VisualElement();
            iconCircle.AddToClassList("fm-reward-icon-circle");
            var iconText = new Text { text = item?.IconEmoji ?? "🎁" };
            iconText.AddToClassList("fm-reward-icon-emoji");
            iconCircle.Add(iconText);
            container.Add(iconCircle);

            var title = new Text { text = item?.Title ?? "" };
            title.AddToClassList("fm-reward-title");
            container.Add(title);

            var subtitle = new Text { text = item?.Subtitle ?? "" };
            subtitle.AddToClassList("fm-reward-subtitle");
            container.Add(subtitle);

            return container;
        }

        /// <summary>
        /// Visual content builder for Pet Item rewards.
        /// Customize this function to alter how Pet Item rewards are displayed.
        /// </summary>
        public static VisualElement BuildPetItemContent(RewardPresentationItem item)
        {
            var container = CreateContentContainer("fm-reward-content--pet-item");

            var iconCircle = new VisualElement();
            iconCircle.AddToClassList("fm-reward-icon-circle");
            var iconText = new Text { text = item?.IconEmoji ?? "🐾" };
            iconText.AddToClassList("fm-reward-icon-emoji");
            iconCircle.Add(iconText);
            container.Add(iconCircle);

            var title = new Text { text = item?.Title ?? "" };
            title.AddToClassList("fm-reward-title");
            container.Add(title);

            var subtitle = new Text { text = item?.Subtitle ?? "" };
            subtitle.AddToClassList("fm-reward-subtitle");
            container.Add(subtitle);

            return container;
        }

        /// <summary>
        /// Visual content builder for Collectible rewards.
        /// Customize this function to alter how Collectible rewards are displayed.
        /// </summary>
        public static VisualElement BuildCollectibleContent(RewardPresentationItem item)
        {
            var container = CreateContentContainer("fm-reward-content--collectible");

            var iconCircle = new VisualElement();
            iconCircle.AddToClassList("fm-reward-icon-circle");
            var iconText = new Text { text = item?.IconEmoji ?? "🏆" };
            iconText.AddToClassList("fm-reward-icon-emoji");
            iconCircle.Add(iconText);
            container.Add(iconCircle);

            var title = new Text { text = item?.Title ?? "" };
            title.AddToClassList("fm-reward-title");
            container.Add(title);

            var subtitle = new Text { text = item?.Subtitle ?? "" };
            subtitle.AddToClassList("fm-reward-subtitle");
            container.Add(subtitle);

            return container;
        }

        /// <summary>
        /// Fallback visual content builder for default/unknown reward types.
        /// </summary>
        public static VisualElement BuildDefaultContent(RewardPresentationItem item)
        {
            var container = CreateContentContainer("fm-reward-content--default");

            var iconCircle = new VisualElement();
            iconCircle.AddToClassList("fm-reward-icon-circle");
            var iconText = new Text { text = item?.IconEmoji ?? "⭐" };
            iconText.AddToClassList("fm-reward-icon-emoji");
            iconCircle.Add(iconText);
            container.Add(iconCircle);

            var title = new Text { text = item?.Title ?? "" };
            title.AddToClassList("fm-reward-title");
            container.Add(title);

            var subtitle = new Text { text = item?.Subtitle ?? "" };
            subtitle.AddToClassList("fm-reward-subtitle");
            container.Add(subtitle);

            return container;
        }

        private static VisualElement CreateContentContainer(string specificClass = null)
        {
            var container = new VisualElement();
            container.AddToClassList("fm-reward-content");
            if (!string.IsNullOrEmpty(specificClass))
            {
                container.AddToClassList(specificClass);
            }
            container.style.width = Length.Percent(100);
            container.style.alignItems = Align.Center;
            container.style.justifyContent = Justify.Center;
            return container;
        }
    }
}
