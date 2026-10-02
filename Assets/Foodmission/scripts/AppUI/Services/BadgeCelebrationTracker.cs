using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace eu.foodmission.platform
{
    /// <summary>
    /// Decides which earned badges still need a celebration. The backend has no inbox, so Home diffs the
    /// earned codes from the gamification profile against the set stored here (PlayerPrefs, per user).
    /// On the first check for a user the set is seeded with what they already have, so a reinstall or a
    /// new device does not replay history. Badges earned within <see cref="RecentWindow"/> are the
    /// exception: they are returned, so a freshly registered user still sees FIRST_STEP.
    /// </summary>
    public class BadgeCelebrationTracker
    {
        public static readonly TimeSpan RecentWindow = TimeSpan.FromMinutes(10);

        public static string StorageKey(string userId) => $"celebrated_badges_{userId}";

        public bool IsFirstRun(string userId)
        {
            return !PlayerPrefs.HasKey(StorageKey(userId));
        }

        /// <summary>
        /// Earned codes not celebrated yet, in <paramref name="earnedCodes"/> order. Does not mark them:
        /// call <see cref="MarkCelebrated"/> once they are queued, so a check that fails halfway retries.
        /// On the first run, <paramref name="details"/> supplies earnedAt; without it everything is seeded.
        /// </summary>
        public List<string> GetNewlyEarned(string userId, string[] earnedCodes, UserBadge[] details, DateTime nowUtc)
        {
            List<string> earned = (earnedCodes ?? Array.Empty<string>())
                .Where(c => !string.IsNullOrEmpty(c))
                .Distinct()
                .ToList();

            if (IsFirstRun(userId))
            {
                List<string> recent = earned.Where(c => IsRecent(c, details, nowUtc)).ToList();
                Save(userId, earned.Except(recent));
                return recent;
            }

            HashSet<string> celebrated = Load(userId);
            return earned.Where(c => !celebrated.Contains(c)).ToList();
        }

        public void MarkCelebrated(string userId, IEnumerable<string> codes)
        {
            HashSet<string> set = Load(userId);
            foreach (string code in codes ?? Enumerable.Empty<string>())
            {
                if (!string.IsNullOrEmpty(code))
                {
                    set.Add(code);
                }
            }
            Save(userId, set);
        }

        private static bool IsRecent(string code, UserBadge[] details, DateTime nowUtc)
        {
            UserBadge badge = details?.FirstOrDefault(b => b != null && b.code == code);
            if (badge?.earnedAt == null)
            {
                return false;
            }
            return nowUtc - badge.earnedAt.Value.ToUniversalTime() <= RecentWindow;
        }

        private static HashSet<string> Load(string userId)
        {
            return new HashSet<string>(
                PlayerPrefs.GetString(StorageKey(userId), "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries),
                StringComparer.Ordinal);
        }

        private static void Save(string userId, IEnumerable<string> codes)
        {
            // An empty string still creates the key, which is what ends the first run.
            PlayerPrefs.SetString(StorageKey(userId), string.Join(",", codes));
            PlayerPrefs.Save();
        }
    }
}
