using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Newtonsoft.Json;

using UnityEngine;

namespace eu.foodmission.platform
{
    /// <summary>Failed missions the user must be told about, and the restart flows (spec 2026-10-02 mission failure).</summary>
    public class MissionFailureService : IMissionFailureService
    {
        private const string KeyPrefix = "mission_failure_ack_";

        private readonly IStoreService _storeService;
        private readonly IQuestService _questService;
        private readonly IMissionService _missionService;
        private readonly ILocalStorageService _storage;

        public MissionFailureService(
            IStoreService storeService,
            IQuestService questService,
            IMissionService missionService,
            ILocalStorageService storage)
        {
            _storeService = storeService;
            _questService = questService;
            _missionService = missionService;
            _storage = storage;
        }

        public async Task<IReadOnlyList<MissionProgress>> GetUnacknowledgedFailuresAsync()
        {
            try
            {
                AppState state = _storeService?.GetAppState();
                if (state == null || string.IsNullOrEmpty(state.userId) || string.IsNullOrEmpty(state.userCurrentQuestId))
                {
                    return Array.Empty<MissionProgress>();
                }

                var (quest, questError) = await _questService.GetQuestAsync(state.userCurrentQuestId);
                if (questError != null || quest?.items == null)
                {
                    return Array.Empty<MissionProgress>();
                }

                var questCodes = new HashSet<string>(
                    quest.items
                        .Where(i => i != null && string.Equals(i.contentType, QuestContentType.Mission, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(i.contentCode))
                        .Select(i => i.contentCode),
                    StringComparer.OrdinalIgnoreCase);

                var (progressList, progressError) = await _missionService.GetUserProgressListAsync();
                if (progressError != null || progressList == null)
                {
                    return Array.Empty<MissionProgress>();
                }

                HashSet<string> acknowledged = Load(state.userId);
                return progressList
                    .Where(p => MissionProgressState.IsFailed(p) && !string.IsNullOrEmpty(p.missionCode) && questCodes.Contains(p.missionCode))
                    .Where(p => !acknowledged.Contains(AckKey(p)))
                    .ToList();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MissionFailureService] GetUnacknowledgedFailuresAsync failed: {ex.Message}");
                return Array.Empty<MissionProgress>();
            }
        }

        public void Acknowledge(MissionProgress progress)
        {
            string userId = _storeService?.GetAppState()?.userId;
            if (progress == null || string.IsNullOrEmpty(progress.missionCode) || string.IsNullOrEmpty(userId))
            {
                return;
            }

            HashSet<string> acknowledged = Load(userId);
            if (acknowledged.Add(AckKey(progress)))
            {
                _storage?.SetValue(KeyPrefix + userId, JsonConvert.SerializeObject(acknowledged.ToList()));
            }
        }

        public async Task<(MissionProgress Result, ApiErrorResponse Error)> RestartAsync(MissionProgress failed)
        {
            if (failed == null || string.IsNullOrEmpty(failed.missionCode))
            {
                return (null, new ApiErrorResponse { message = "Mission code is required" });
            }

            Acknowledge(failed);
            return await _missionService.RestartMissionProgressAsync(failed.missionCode);
        }

        public async Task<(MissionProgress Result, ApiErrorResponse Error)> GiveUpAndRestartAsync(string missionCode)
        {
            var (failed, failError) = await _missionService.FailMissionProgressAsync(missionCode);
            if (failError != null)
            {
                return (null, failError);
            }

            if (failed != null && string.IsNullOrEmpty(failed.missionCode))
            {
                failed.missionCode = missionCode;
            }

            // The user gave it up on purpose: it is not news for the Home notice, even if the restart fails now
            Acknowledge(failed);
            return await _missionService.RestartMissionProgressAsync(missionCode);
        }

        private static string AckKey(MissionProgress progress)
        {
            long ticks = progress.startedAt?.ToUniversalTime().Ticks ?? 0L;
            return $"{progress.missionCode.ToUpperInvariant()}|{ticks}";
        }

        private HashSet<string> Load(string userId)
        {
            string json = _storage?.GetValue<string>(KeyPrefix + userId, null);
            if (string.IsNullOrEmpty(json))
            {
                return new HashSet<string>(StringComparer.Ordinal);
            }

            try
            {
                return new HashSet<string>(JsonConvert.DeserializeObject<List<string>>(json) ?? new List<string>(), StringComparer.Ordinal);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MissionFailureService] Could not read acknowledged failures: {ex.Message}");
                return new HashSet<string>(StringComparer.Ordinal);
            }
        }
    }
}
