using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Newtonsoft.Json;

using UnityEngine;

namespace eu.foodmission.platform
{
    /// <summary>Client-side detection of missing check-in days and stalled missions for the Home nudge (spec §6).</summary>
    public class MissionNudgeService : IMissionNudgeService
    {
        public static readonly TimeSpan StallThreshold = TimeSpan.FromDays(2);
        public static readonly TimeSpan NudgeCooldown = TimeSpan.FromHours(24);
        public const int MissingDaysThreshold = 2;
        private const string KeyPrefix = "mission_nudge_";
        private const string MissingDaysKey = "__missing_days__";
        private const string QuestSeenKeyPrefix = "__quest_seen__";

        [Serializable]
        private class MissionNudgeState
        {
            public DateTime FirstSeenAtUtc;
            public float LastProgress;
            public DateTime LastChangeAtUtc;
            public DateTime? LastNudgeAtUtc;
        }

        private readonly IStoreService _storeService;
        private readonly IQuestService _questService;
        private readonly IMissionService _missionService;
        private readonly ILocalStorageService _storage;
        private readonly ICheckInService _checkInService;

        public Func<DateTime> UtcNow { get; set; } = () => DateTime.UtcNow;
        public Func<DateTime> NowLocal { get; set; } = () => DateTime.Now;

        public MissionNudgeService(
            IStoreService storeService,
            IQuestService questService,
            IMissionService missionService,
            ILocalStorageService storage,
            ICheckInService checkInService)
        {
            _storeService = storeService;
            _questService = questService;
            _missionService = missionService;
            _storage = storage;
            _checkInService = checkInService;
        }

        public async Task<MissionNudge> GetNudgeAsync()
        {
            try
            {
                AppState state = _storeService?.GetAppState();
                if (state == null || string.IsNullOrEmpty(state.userId) || string.IsNullOrEmpty(state.userCurrentQuestId))
                {
                    return null;
                }

                Dictionary<string, MissionNudgeState> states = Load(state.userId);
                DateTime now = UtcNow();

                MissionNudge nudge = await GetMissingDaysNudgeAsync(state.userCurrentQuestId, states, now)
                    ?? await GetStalledMissionNudgeAsync(state.userCurrentQuestId, states, now);

                Save(state.userId, states);
                return nudge;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MissionNudgeService] GetNudgeAsync failed: {ex.Message}");
                return null;
            }
        }

        public void Reset()
        {
            string userId = _storeService?.GetAppState()?.userId;
            if (!string.IsNullOrEmpty(userId))
            {
                _storage?.DeleteValue(KeyPrefix + userId);
            }
        }

        private async Task<MissionNudge> GetMissingDaysNudgeAsync(string questId, Dictionary<string, MissionNudgeState> states, DateTime now)
        {
            // startedAt is null until the first progress, so remember when this quest was first seen as current:
            // days before that are not "missing"
            string questKey = QuestSeenKeyPrefix + questId;
            if (!states.TryGetValue(questKey, out MissionNudgeState questState))
            {
                questState = new MissionNudgeState { FirstSeenAtUtc = now, LastChangeAtUtc = now };
                states[questKey] = questState;
            }

            if (_checkInService == null || IsOnCooldown(states, MissingDaysKey, now))
            {
                return null;
            }

            var (plan, error) = await _checkInService.LoadPlanAsync();
            if (error != null || plan == null)
            {
                return null;
            }

            int days = plan.PendingPastDays(NowLocal(), questState.FirstSeenAtUtc.ToLocalTime());
            if (days < MissingDaysThreshold)
            {
                return null;
            }

            MarkNudged(states, MissingDaysKey, now);
            return MissionNudge.ForMissingDays(days);
        }

        private async Task<MissionNudge> GetStalledMissionNudgeAsync(string questId, Dictionary<string, MissionNudgeState> states, DateTime now)
        {
            var (quest, questError) = await _questService.GetQuestAsync(questId);
            if (questError != null || quest?.items == null)
            {
                return null;
            }

            List<string> codes = quest.items
                .Where(i => i != null && string.Equals(i.contentType, QuestContentType.Mission, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(i.contentCode))
                .Select(i => i.contentCode)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (codes.Count == 0)
            {
                return null;
            }

            var (progressList, progressError) = await _missionService.GetUserProgressListAsync();
            if (progressError != null)
            {
                return null;
            }

            Dictionary<string, MissionProgress> byCode = (progressList ?? Array.Empty<MissionProgress>())
                .Where(p => !string.IsNullOrEmpty(p?.missionCode))
                .GroupBy(p => p.missionCode, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            string bestCode = null;
            TimeSpan bestStall = TimeSpan.Zero;
            foreach (string code in codes)
            {
                if (!MissionInteractionCatalog.Get(code).CanReport)
                {
                    continue;
                }

                byCode.TryGetValue(code, out MissionProgress progress);
                float value = progress?.progress ?? 0f;
                if (progress?.completed == true || value >= 100f)
                {
                    continue;
                }

                if (!states.TryGetValue(code, out MissionNudgeState missionState))
                {
                    DateTime baseline = progress?.startedAt?.ToUniversalTime() ?? now;
                    if (baseline > now)
                    {
                        baseline = now;
                    }
                    missionState = new MissionNudgeState { FirstSeenAtUtc = now, LastProgress = value, LastChangeAtUtc = baseline };
                    states[code] = missionState;
                }
                else if (Math.Abs(missionState.LastProgress - value) > 0.01f)
                {
                    missionState.LastProgress = value;
                    missionState.LastChangeAtUtc = now;
                    continue;
                }

                if (IsOnCooldown(states, code, now))
                {
                    continue;
                }

                TimeSpan stalled = now - missionState.LastChangeAtUtc;
                if (stalled >= StallThreshold && stalled > bestStall)
                {
                    bestStall = stalled;
                    bestCode = code;
                }
            }

            if (bestCode == null)
            {
                return null;
            }

            MarkNudged(states, bestCode, now);
            byCode.TryGetValue(bestCode, out MissionProgress best);
            string title = !string.IsNullOrEmpty(best?.missionTitle) ? best.missionTitle : bestCode;
            return MissionNudge.ForStalledMission(bestCode, title, MissionInteractionCatalog.Get(bestCode).AutoModules.FirstOrDefault());
        }

        private static bool IsOnCooldown(Dictionary<string, MissionNudgeState> states, string key, DateTime now) =>
            states.TryGetValue(key, out MissionNudgeState s) && s.LastNudgeAtUtc.HasValue && now - s.LastNudgeAtUtc.Value < NudgeCooldown;

        private static void MarkNudged(Dictionary<string, MissionNudgeState> states, string key, DateTime now)
        {
            if (!states.TryGetValue(key, out MissionNudgeState s))
            {
                s = new MissionNudgeState { FirstSeenAtUtc = now, LastChangeAtUtc = now };
                states[key] = s;
            }
            s.LastNudgeAtUtc = now;
        }

        private Dictionary<string, MissionNudgeState> Load(string userId)
        {
            string json = _storage?.GetValue<string>(KeyPrefix + userId, null);
            if (string.IsNullOrEmpty(json))
            {
                return new Dictionary<string, MissionNudgeState>(StringComparer.OrdinalIgnoreCase);
            }

            try
            {
                var loaded = JsonConvert.DeserializeObject<Dictionary<string, MissionNudgeState>>(json);
                return new Dictionary<string, MissionNudgeState>(loaded ?? new Dictionary<string, MissionNudgeState>(), StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MissionNudgeService] Corrupt nudge state, resetting: {ex.Message}");
                return new Dictionary<string, MissionNudgeState>(StringComparer.OrdinalIgnoreCase);
            }
        }

        private void Save(string userId, Dictionary<string, MissionNudgeState> states)
        {
            _storage?.SetValue(KeyPrefix + userId, JsonConvert.SerializeObject(states));
        }
    }
}
