using System;

namespace eu.foodmission.platform
{
    [Serializable]
    public class WalletBalance
    {
        public int xp;
        public int points;
        public string updatedAt;
    }

    [Serializable]
    public class ContentReward
    {
        public int? xp;
        public int? points;
        public string badgeId;
        public string avatarItem;
        public string petItem;
        public string collectible;
    }

    [Serializable]
    public class WalletEntry
    {
        public string id;
        public string currency; // "XP" | "POINTS"
        public int amount;
        public int balanceAfter;
        public string reason;
        public string eventId;
        public string createdAt;
    }

    [Serializable]
    public class ProgressIndicator
    {
        public string id;
        public string kind;
        public string precision;
        public int level;
        public float accumulatedValue;
        public float targetValue;
        public float allTimeTotal;
        public string cycleStartedAt;
        public string lastUpdatedAt;
    }

    [Serializable]
    public class EarnedRewardDetail
    {
        public string id;
        public string name;
        public int? points;
        public int? xp;
        public string badgeId;
        public string avatarItem;
        public string petItem;
        public string collectible;
    }

    [Serializable]
    public class EarnedRewardItem
    {
        public string id;
        public EarnedRewardDetail reward;
        public string sourceType; // "MISSION" | "CHALLENGE" | "QUEST" | "QUIZ" | "FOOD_FACT"
        public string sourceId;
        public string earnedAt;
    }

    [Serializable]
    public class UserEarnedRewardsResponse
    {
        public EarnedRewardItem[] earnedRewards;
        public WalletBalance wallet;
    }

    [Serializable]
    public class GamificationProfileResponse
    {
        public string userId;
        public string segment;
        public string currentQuestId;
        public string lastLoginAt;
        public WalletBalance wallet;
        public ProgressIndicator[] progressIndicators;
        public string[] badges;
        public WalletEntry[] recentWalletEntries;
    }
}
