using System;
using System.Collections.Generic;
using System.Linq;
using MainraGames;
using Unity.AppUI.Core;
using Unity.AppUI.MVVM;
using Unity.AppUI.UI;
using UnityEngine;
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
    }

    /// <summary>
    /// Global full-screen celebratory modal displayed when a user earns gamification rewards
    /// (XP, Points, Badges, Avatar items). Features the 3D Avatar in FullBody celebration mode,
    /// particle effects, and animated sequential presentation cards with tap-to-advance.
    /// </summary>
    public static class RewardCelebrationDialog
    {
        private static IVisualElementScheduledItem s_AutoAdvanceSchedule;
        private static bool s_IsAdvancing;
        private static Modal s_CurrentModal;

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
                    Subtitle = "@UI:REWARD_XP_EARNED",
                    IconEmoji = "⭐",
                    Value = reward.xp.Value
                });
            }

            if (reward.points.HasValue && reward.points.Value > 0)
            {
                list.Add(new RewardPresentationItem
                {
                    Type = RewardType.Points,
                    Title = $"+{reward.points.Value} Pts",
                    Subtitle = "@UI:REWARD_POINTS_EARNED",
                    IconEmoji = "🌱",
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
                    Value = 1
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
                    Value = 1
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
                    Value = 1
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
                    Value = 1
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

            ShowModal(rootElement, queue, contextTitle, onDismiss);
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
                s_AutoAdvanceSchedule?.Pause();
                s_AutoAdvanceSchedule = null;
                s_CurrentModal.Dismiss(DismissType.Action);
                s_CurrentModal = null;
            }

            s_IsAdvancing = false;

            // Root Modal Container
            var root = new VisualElement();
            root.AddToClassList("fm-reward-modal-root");

            // Avatar & Stage Background Area
            var avatarStage = new VisualElement();
            avatarStage.AddToClassList("fm-reward-avatar-stage");
            avatarStage.pickingMode = PickingMode.Ignore;

            // Particles
            UIParticle particles = null;
            try
            {
                particles = new UIParticle { name = "particles" };
                particles.style.position = Position.Absolute;
                particles.style.top = 0;
                particles.style.left = 0;
                particles.style.right = 0;
                particles.style.bottom = 0;
#if UNITY_EDITOR
                var profile = UnityEditor.AssetDatabase.LoadAssetAtPath<UIParticleProfile>("Assets/Foodmission/particles/UIParticles-QuizWin.asset");
                if (profile != null)
                {
                    particles.Profile = profile;
                }
#endif
                avatarStage.Add(particles);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RewardCelebrationDialog] Could not initialize UIParticle: {ex.Message}");
            }

            // Spotlight Image
            var spotlight = new Image { scaleMode = ScaleMode.StretchToFill };
            spotlight.style.position = Position.Absolute;
            spotlight.style.bottom = 0;
            spotlight.style.top = -200;
            spotlight.style.width = 800;
            spotlight.style.opacity = 0.8f;
#if UNITY_EDITOR
            var focoSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Foodmission/graphics/png/foco_avatar.png");
            if (focoSprite != null) spotlight.sprite = focoSprite;
#endif
            avatarStage.Add(spotlight);

            // FullBody Avatar
            var avatarView = new FMAvatarView { Mode = AvatarViewMode.FullBody };
            avatarView.style.flexGrow = 1;
            avatarView.style.alignSelf = Align.Center;
            avatarView.style.width = Length.Percent(100);
            avatarView.style.height = Length.Percent(100);
            avatarStage.Add(avatarView);

            // Stand Image
            var stand = new Image { scaleMode = ScaleMode.ScaleAndCrop };
            stand.style.position = Position.Absolute;
            stand.style.bottom = 0;
            stand.style.width = 320;
            stand.style.height = 100;
#if UNITY_EDITOR
            var standSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Foodmission/graphics/png/stand.png");
            if (standSprite != null) stand.sprite = standSprite;
#endif
            avatarStage.Add(stand);

            root.Add(avatarStage);

            // Header Container & Title
            var headerContainer = new VisualElement();
            headerContainer.AddToClassList("fm-reward-header-container");
            var headerTitle = new Text();
            headerTitle.AddToClassList("fm-reward-celebration-title");
            headerTitle.text = !string.IsNullOrEmpty(contextTitle) ? contextTitle : "@UI:REWARD_CELEBRATION_TITLE";
            headerContainer.Add(headerTitle);
            root.Add(headerContainer);

            // Center Reward Card Container
            var cardContainer = new VisualElement();
            cardContainer.AddToClassList("fm-reward-card-container");

            var rewardCard = new ExVisualElement();
            rewardCard.AddToClassList("box-background");
            rewardCard.AddToClassList("fm-shadow-wrapper");
            rewardCard.AddToClassList("fm-reward-card");

            var iconCircle = new VisualElement();
            iconCircle.AddToClassList("fm-reward-icon-circle");
            var iconEmojiText = new Text();
            iconEmojiText.AddToClassList("fm-reward-icon-emoji");
            iconCircle.Add(iconEmojiText);
            rewardCard.Add(iconCircle);

            var rewardTitleText = new Text();
            rewardTitleText.AddToClassList("fm-reward-title");
            rewardCard.Add(rewardTitleText);

            var rewardSubtitleText = new Text();
            rewardSubtitleText.AddToClassList("fm-reward-subtitle");
            rewardCard.Add(rewardSubtitleText);

            // Dots indicators if more than 1 item
            var dotsContainer = new VisualElement();
            dotsContainer.AddToClassList("fm-reward-dots");
            var dotElements = new List<VisualElement>();
            if (queue.Count > 1)
            {
                for (int i = 0; i < queue.Count; i++)
                {
                    var dot = new VisualElement();
                    dot.AddToClassList("fm-reward-dot");
                    dotsContainer.Add(dot);
                    dotElements.Add(dot);
                }
                rewardCard.Add(dotsContainer);
            }

            cardContainer.Add(rewardCard);
            root.Add(cardContainer);

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
            root.Add(bottomContainer);

            // Apply Safe Area
            App.current?.services?.GetService<IThemeService>()?.ApplySafeAreaPadding(root, true, true, false, false);

            // Audio & Mascot Feedback
            var audioService = App.current?.services?.GetService<IAudioService>();
            var sfx = queue.Any(q => q.Type == RewardType.Badge) ? SfxType.WinBadge : SfxType.QuizPositive;
            audioService?.PlaySfx(sfx);

            var avatarService = App.current?.services?.GetService<IAvatarService>();
            if (avatarService?.AvatarController?.AvatarAnimationController != null)
            {
                avatarService.AvatarController.AvatarAnimationController.CurrentMood = AvatarMood.Happy;
                avatarService.AvatarController.AvatarAnimationController.TriggerCelebration();
            }

            if (particles != null)
            {
                particles.style.display = DisplayStyle.Flex;
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

                s_AutoAdvanceSchedule?.Pause();
                s_AutoAdvanceSchedule = null;
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
                iconEmojiText.text = item.IconEmoji ?? "⭐";
                rewardTitleText.text = item.Title ?? "";
                rewardSubtitleText.text = item.Subtitle ?? "";

                // Update Dots
                for (int k = 0; k < dotElements.Count; k++)
                {
                    if (k == index)
                    {
                        dotElements[k].AddToClassList("fm-reward-dot--active");
                    }
                    else
                    {
                        dotElements[k].RemoveFromClassList("fm-reward-dot--active");
                    }
                }

                // Update Button Text on last item
                if (index == queue.Count - 1)
                {
                    btnContinue.title = "@UI:TXT_FINISH";
                }
                else
                {
                    btnContinue.title = "@UI:TXT_CONTINUE";
                }

                // Slide In
                rewardCard.RemoveFromClassList("fm-reward-card--exit");
                rewardCard.AddToClassList("fm-reward-card--visible");

                // Auto-advance after 1.8 seconds
                s_AutoAdvanceSchedule?.Pause();
                s_AutoAdvanceSchedule = root.schedule.Execute(Advance).StartingIn(1800);
            }

            void Advance()
            {
                if (isDismissed || s_IsAdvancing) return;
                s_IsAdvancing = true;

                s_AutoAdvanceSchedule?.Pause();
                s_AutoAdvanceSchedule = null;

                // Animate Slide Out
                rewardCard.RemoveFromClassList("fm-reward-card--visible");
                rewardCard.AddToClassList("fm-reward-card--exit");

                root.schedule.Execute(() =>
                {
                    currentIndex++;
                    if (currentIndex < queue.Count)
                    {
                        s_IsAdvancing = false;
                        DisplayItem(currentIndex);
                    }
                    else
                    {
                        DismissAndCleanup();
                    }
                }).StartingIn(250);
            }

            // Click Handlers
            btnContinue.clicked += () => Advance();
            rewardCard.RegisterCallback<ClickEvent>(evt =>
            {
                evt.StopPropagation();
                Advance();
            });

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
    }
}
