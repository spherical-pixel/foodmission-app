using System;

using Newtonsoft.Json;

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

        /// <summary>Display name of the badge, filled client-side for badge celebrations. Never sent or received.</summary>
        [JsonIgnore]
        public string badgeName { get; set; }
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
        public string sourceType;
        public string sourceId;
        public string createdAt;
    }

    /// <summary>
    /// Sustainability progress wheel from GET /users/me/gamification/progress-wheels
    /// (CO2_REDUCTION, ENERGY_REDUCTION, WATER_SAVINGS, LAND_USE_REDUCTION).
    /// </summary>
    [Serializable]
    public class ProgressWheel
    {
        public string id;
        public string kind;
        public string label;
        public string unit;
        public string profile;
        public int stage;
        public string stageTitle;
        public float sustainabilityTargetPercent;
        public float accumulatedValue;
        public float targetValue;
        public float percentComplete;
        public float allTimeTotal;
        public string cycleStartedAt;
        public string lastUpdatedAt;
    }

    /// <summary>Response of POST /users/me/gamification/onboarding-survey.</summary>
    [Serializable]
    public class OnboardingSurveyResult
    {
        public string segment;
        public ProgressWheel[] progressWheels;
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
        public UserEvent[] recentEvents;
        public WalletEntry[] recentWalletEntries;
    }

    [Serializable]
    public class UserBadge
    {
        public string code;
        public string name;
        public string description;
        public string imageUrl;
        public int sortOrder;
        public string ruleCode;
        public bool earned;
        // DateTime? (not string) so Newtonsoft does not reformat the ISO date with the device culture.
        public DateTime? earnedAt;
        // 0–100, server-derived; always 100 when earned.
        public float progress;
        public string status;
    }

    [Serializable]
    public class UserBadgesResponse
    {
        public UserBadge[] badges;
        public int earnedCount;
        public int totalCount;
    }
}
