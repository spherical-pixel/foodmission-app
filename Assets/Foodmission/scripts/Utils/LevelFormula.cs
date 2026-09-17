using System.Collections.Generic;
using UnityEngine;

namespace eu.foodmission.platform.Utils
{
    public struct LevelProgressInfo
    {
        public int Level;
        public int CurrentXpInLevel;
        public int XpNeededInLevel;
        public float NormalizedProgress; // bar.value (0 to 1)
    }

    public struct XpAnimationStep
    {
        public int Level;
        public float StartProgress;
        public float EndProgress;
        public bool IsLevelUp;
    }

    public static class LevelFormula
    {
        /// <summary>
        /// Calculate the level based on the total accumulated XP.
        /// </summary>
        public static int GetLevel(int totalXp)
        {
            if (totalXp <= 0) return 1;
            return Mathf.FloorToInt((Mathf.Sqrt(225f + 20f * totalXp) - 5f) / 10f);
        }
        /// <summary>
        /// Calculate the total accumulated XP required to reach the start of the specified level.
        /// </summary>
        public static int GetTotalXpForLevel(int level)
        {
            if (level <= 1) return 0;
            return (5 * level * level) + (5 * level) - 10;
        }
        /// <summary>
        /// Calculate the XP required for the specified level.
        /// bar_points = ((level - 1) * 10) + 20
        /// </summary>
        public static int GetXpRequiredForLevel(int level)
        {
            return ((level - 1) * 10) + 20;
        }
        /// <summary>
        /// Get detailed information about the current level for animating the progress bar.
        /// </summary>
        public static LevelProgressInfo GetProgressInfo(int totalXp)
        {
            int level = GetLevel(totalXp);
            int startXpOfCurrentLevel = GetTotalXpForLevel(level);
            int xpNeededInCurrentLevel = GetXpRequiredForLevel(level);
            int currentXpInLevel = totalXp - startXpOfCurrentLevel;
            float progress = Mathf.Clamp01((float)currentXpInLevel / xpNeededInCurrentLevel);
            return new LevelProgressInfo
            {
                Level = level,
                CurrentXpInLevel = currentXpInLevel,
                XpNeededInLevel = xpNeededInCurrentLevel,
                NormalizedProgress = progress // 0.0f to 1.0f
            };
        }

        /// <summary>
        /// Calculates discrete sequential visual steps for animating the XP progress bar from startXp to endXp.
        /// Handles progress within the same level as well as single or multi-level advancements.
        /// </summary>
        public static List<XpAnimationStep> CalculateAnimationSteps(int startXp, int endXp)
        {
            var steps = new List<XpAnimationStep>();
            startXp = Mathf.Max(0, startXp);
            endXp = Mathf.Max(0, endXp);

            if (endXp <= startXp)
            {
                return steps;
            }

            var startInfo = GetProgressInfo(startXp);
            var endInfo = GetProgressInfo(endXp);

            if (startInfo.Level == endInfo.Level)
            {
                steps.Add(new XpAnimationStep
                {
                    Level = startInfo.Level,
                    StartProgress = startInfo.NormalizedProgress,
                    EndProgress = endInfo.NormalizedProgress,
                    IsLevelUp = false
                });
                return steps;
            }

            // Step 1: Complete current level up to 100%
            steps.Add(new XpAnimationStep
            {
                Level = startInfo.Level,
                StartProgress = startInfo.NormalizedProgress,
                EndProgress = 1.0f,
                IsLevelUp = true
            });

            // Intermediate full levels (if any)
            for (int lvl = startInfo.Level + 1; lvl < endInfo.Level; lvl++)
            {
                steps.Add(new XpAnimationStep
                {
                    Level = lvl,
                    StartProgress = 0.0f,
                    EndProgress = 1.0f,
                    IsLevelUp = true
                });
            }

            // Final level (if there is remaining progress in the target level)
            if (endInfo.NormalizedProgress > 0f)
            {
                steps.Add(new XpAnimationStep
                {
                    Level = endInfo.Level,
                    StartProgress = 0.0f,
                    EndProgress = endInfo.NormalizedProgress,
                    IsLevelUp = false
                });
            }

            return steps;
        }
    }
}