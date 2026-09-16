using System.Collections.Generic;
using eu.foodmission.platform;
using eu.foodmission.platform.Components;
using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class RewardCelebrationDialogTests
    {
        [Test]
        public void BuildPresentationQueue_WhenRewardNull_ReturnsEmptyList()
        {
            var queue = RewardCelebrationDialog.BuildPresentationQueue(null);
            Assert.IsNotNull(queue);
            Assert.AreEqual(0, queue.Count);
        }

        [Test]
        public void BuildPresentationQueue_WhenRewardHasPositiveXp_CreatesXpItemWithCorrectProperties()
        {
            var reward = new ContentReward { xp = 50 };
            var queue = RewardCelebrationDialog.BuildPresentationQueue(reward);

            Assert.AreEqual(1, queue.Count);
            var item = queue[0];
            Assert.AreEqual(RewardType.Xp, item.Type);
            Assert.AreEqual("+50 XP", item.Title);
            Assert.AreEqual("⭐", item.IconEmoji);
            Assert.AreEqual(50, item.Value);
            Assert.AreEqual("@UI:REWARD_XP_EARNED", item.Subtitle);
        }

        [Test]
        public void BuildPresentationQueue_WhenRewardHasPositivePoints_CreatesPointsItemWithCorrectProperties()
        {
            var reward = new ContentReward { points = 10 };
            var queue = RewardCelebrationDialog.BuildPresentationQueue(reward);

            Assert.AreEqual(1, queue.Count);
            var item = queue[0];
            Assert.AreEqual(RewardType.Points, item.Type);
            Assert.AreEqual("+10 Pts", item.Title);
            Assert.AreEqual("🌱", item.IconEmoji);
            Assert.AreEqual(10, item.Value);
            Assert.AreEqual("@UI:REWARD_POINTS_EARNED", item.Subtitle);
        }

        [Test]
        public void BuildPresentationQueue_WhenRewardHasBothXpAndPoints_ReturnsInSequentialOrder()
        {
            var reward = new ContentReward { xp = 100, points = 25 };
            var queue = RewardCelebrationDialog.BuildPresentationQueue(reward);

            Assert.AreEqual(2, queue.Count);
            Assert.AreEqual(RewardType.Xp, queue[0].Type);
            Assert.AreEqual(RewardType.Points, queue[1].Type);
            Assert.AreEqual(100, queue[0].Value);
            Assert.AreEqual(25, queue[1].Value);
        }

        [Test]
        public void BuildPresentationQueue_WhenRewardHasBadge_CreatesBadgeItem()
        {
            var reward = new ContentReward { badgeId = "ECO_CHAMPION" };
            var queue = RewardCelebrationDialog.BuildPresentationQueue(reward);

            Assert.AreEqual(1, queue.Count);
            var item = queue[0];
            Assert.AreEqual(RewardType.Badge, item.Type);
            Assert.AreEqual("@UI:REWARD_BADGE_UNLOCKED", item.Title);
            Assert.AreEqual("ECO_CHAMPION", item.Subtitle);
            Assert.AreEqual("🏅", item.IconEmoji);
        }

        [Test]
        public void BuildPresentationQueue_WhenRewardHasAvatarItem_CreatesAvatarItem()
        {
            var reward = new ContentReward { avatarItem = "CHEF_HAT" };
            var queue = RewardCelebrationDialog.BuildPresentationQueue(reward);

            Assert.AreEqual(1, queue.Count);
            var item = queue[0];
            Assert.AreEqual(RewardType.AvatarItem, item.Type);
            Assert.AreEqual("@UI:REWARD_AVATAR_ITEM", item.Title);
            Assert.AreEqual("CHEF_HAT", item.Subtitle);
            Assert.AreEqual("🎁", item.IconEmoji);
        }

        [Test]
        public void BuildPresentationQueue_WhenRewardHasPetItem_CreatesPetItem()
        {
            var reward = new ContentReward { petItem = "PET_COLLAR" };
            var queue = RewardCelebrationDialog.BuildPresentationQueue(reward);

            Assert.AreEqual(1, queue.Count);
            var item = queue[0];
            Assert.AreEqual(RewardType.PetItem, item.Type);
            Assert.AreEqual("@UI:REWARD_PET_ITEM", item.Title);
            Assert.AreEqual("PET_COLLAR", item.Subtitle);
            Assert.AreEqual("🐾", item.IconEmoji);
        }

        [Test]
        public void BuildPresentationQueue_WhenRewardHasCollectible_CreatesCollectibleItem()
        {
            var reward = new ContentReward { collectible = "GOLDEN_APPLE" };
            var queue = RewardCelebrationDialog.BuildPresentationQueue(reward);

            Assert.AreEqual(1, queue.Count);
            var item = queue[0];
            Assert.AreEqual(RewardType.Collectible, item.Type);
            Assert.AreEqual("@UI:REWARD_COLLECTIBLE", item.Title);
            Assert.AreEqual("GOLDEN_APPLE", item.Subtitle);
            Assert.AreEqual("🏆", item.IconEmoji);
        }

        [Test]
        public void BuildPresentationQueue_WhenZeroOrNegative_OmitsZeroValues()
        {
            var reward = new ContentReward { xp = 0, points = -5 };
            var queue = RewardCelebrationDialog.BuildPresentationQueue(reward);

            Assert.AreEqual(0, queue.Count);
        }

        [Test]
        public void BuildPresentationQueue_WhenAllRewardTypesPresent_ReturnsAllInOrder()
        {
            var reward = new ContentReward
            {
                xp = 50,
                points = 15,
                badgeId = "FIRST_QUIZ",
                avatarItem = "GREEN_CAP",
                petItem = "BOWL",
                collectible = "TROPHY_1"
            };

            var queue = RewardCelebrationDialog.BuildPresentationQueue(reward);

            Assert.AreEqual(6, queue.Count);
            Assert.AreEqual(RewardType.Xp, queue[0].Type);
            Assert.AreEqual(RewardType.Points, queue[1].Type);
            Assert.AreEqual(RewardType.Badge, queue[2].Type);
            Assert.AreEqual(RewardType.AvatarItem, queue[3].Type);
            Assert.AreEqual(RewardType.PetItem, queue[4].Type);
            Assert.AreEqual(RewardType.Collectible, queue[5].Type);
        }

        [Test]
        public void Show_WhenRewardNull_InvokesOnDismissImmediately()
        {
            bool dismissed = false;
            RewardCelebrationDialog.Show(null, null, () => dismissed = true);

            Assert.IsTrue(dismissed);
        }

        [Test]
        public void Show_WhenRewardHasNoPositiveValues_InvokesOnDismissImmediately()
        {
            bool dismissed = false;
            var reward = new ContentReward { xp = 0, points = 0 };
            RewardCelebrationDialog.Show(reward, null, () => dismissed = true);

            Assert.IsTrue(dismissed);
        }

        [Test]
        public void CreateRewardVisual_WhenItemNull_ReturnsEmptyElement()
        {
            var visual = RewardCelebrationDialog.CreateRewardVisual(null);
            Assert.IsNotNull(visual);
            Assert.AreEqual(0, visual.childCount);
        }

        [Test]
        public void CreateRewardVisual_WhenXp_ReturnsXpContentWithCorrectClass()
        {
            var item = new RewardPresentationItem
            {
                Type = RewardType.Xp,
                Title = "+50 XP",
                Subtitle = "@UI:REWARD_XP_EARNED",
                IconEmoji = "⭐",
                Value = 50
            };

            var visual = RewardCelebrationDialog.CreateRewardVisual(item);
            Assert.IsNotNull(visual);
            Assert.IsTrue(visual.ClassListContains("fm-reward-content--xp"));
            Assert.IsTrue(visual.ClassListContains("fm-reward-content"));
        }

        [Test]
        public void CreateRewardVisual_WhenPoints_ReturnsPointsContentWithCorrectClass()
        {
            var item = new RewardPresentationItem
            {
                Type = RewardType.Points,
                Title = "+10 Pts",
                Subtitle = "@UI:REWARD_POINTS_EARNED",
                IconEmoji = "🌱",
                Value = 10
            };

            var visual = RewardCelebrationDialog.CreateRewardVisual(item);
            Assert.IsNotNull(visual);
            Assert.IsTrue(visual.ClassListContains("fm-reward-content--points"));
        }

        [Test]
        public void CreateRewardVisual_WhenBadge_ReturnsBadgeContentWithCorrectClass()
        {
            var item = new RewardPresentationItem
            {
                Type = RewardType.Badge,
                Title = "@UI:REWARD_BADGE_UNLOCKED",
                Subtitle = "ECO_HERO",
                IconEmoji = "🏅",
                Value = 1,
                RawId = "ECO_HERO"
            };

            var visual = RewardCelebrationDialog.CreateRewardVisual(item);
            Assert.IsNotNull(visual);
            Assert.IsTrue(visual.ClassListContains("fm-reward-content--badge"));
        }

        [Test]
        public void CreateRewardVisual_WhenAvatarItem_ReturnsAvatarItemContentWithCorrectClass()
        {
            var item = new RewardPresentationItem
            {
                Type = RewardType.AvatarItem,
                Title = "@UI:REWARD_AVATAR_ITEM",
                Subtitle = "GREEN_HAT",
                IconEmoji = "🎁",
                Value = 1,
                RawId = "GREEN_HAT"
            };

            var visual = RewardCelebrationDialog.CreateRewardVisual(item);
            Assert.IsNotNull(visual);
            Assert.IsTrue(visual.ClassListContains("fm-reward-content--avatar-item"));
        }

        [Test]
        public void CreateRewardVisual_WhenPetItem_ReturnsPetItemContentWithCorrectClass()
        {
            var item = new RewardPresentationItem
            {
                Type = RewardType.PetItem,
                Title = "@UI:REWARD_PET_ITEM",
                Subtitle = "COLLAR",
                IconEmoji = "🐾",
                Value = 1
            };

            var visual = RewardCelebrationDialog.CreateRewardVisual(item);
            Assert.IsNotNull(visual);
            Assert.IsTrue(visual.ClassListContains("fm-reward-content--pet-item"));
        }

        [Test]
        public void CreateRewardVisual_WhenCollectible_ReturnsCollectibleContentWithCorrectClass()
        {
            var item = new RewardPresentationItem
            {
                Type = RewardType.Collectible,
                Title = "@UI:REWARD_COLLECTIBLE",
                Subtitle = "CUP",
                IconEmoji = "🏆",
                Value = 1
            };

            var visual = RewardCelebrationDialog.CreateRewardVisual(item);
            Assert.IsNotNull(visual);
            Assert.IsTrue(visual.ClassListContains("fm-reward-content--collectible"));
        }
    }
}
