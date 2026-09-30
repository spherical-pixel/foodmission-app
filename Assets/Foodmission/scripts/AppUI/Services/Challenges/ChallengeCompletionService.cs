using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using UnityEngine;

namespace eu.foodmission.platform
{
    public class ChallengeCompletionService : IChallengeCompletionService
    {
        private readonly IChallengeService _challengeService;
        private readonly IAuthService _authService;
        private readonly Dictionary<string, Task<ChallengeCompletionResult>> _inFlight = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _completed = new(StringComparer.OrdinalIgnoreCase);
        // Incremented on Reset so requests started by a previous user cannot write into the new session
        private int _generation;

        public event Action<string, ContentReward> ChallengeCompleted;

        public ChallengeCompletionService(IChallengeService challengeService, IAuthService authService)
        {
            _challengeService = challengeService;
            _authService = authService;
        }

        public bool IsCompleted(string challengeCode)
        {
            return !string.IsNullOrWhiteSpace(challengeCode) && _completed.Contains(challengeCode.Trim());
        }

        public void Reset()
        {
            _generation++;
            _completed.Clear();
            _inFlight.Clear();
        }

        public Task<ChallengeCompletionResult> CompleteAsync(string challengeCode)
        {
            if (string.IsNullOrWhiteSpace(challengeCode) || _challengeService == null)
            {
                return Task.FromResult(new ChallengeCompletionResult { Success = false });
            }

            string code = challengeCode.Trim();
            if (_completed.Contains(code))
            {
                return Task.FromResult(new ChallengeCompletionResult { Success = true });
            }

            if (_inFlight.TryGetValue(code, out Task<ChallengeCompletionResult> pending))
            {
                return JoinAsync(pending);
            }

            Task<ChallengeCompletionResult> task = SendAsync(code);
            if (!task.IsCompleted)
            {
                _inFlight[code] = task;
            }
            return task;
        }

        // Callers joining an in-flight request share its outcome but not its reward, so it is celebrated once
        private static async Task<ChallengeCompletionResult> JoinAsync(Task<ChallengeCompletionResult> pending)
        {
            ChallengeCompletionResult result = await pending;
            return new ChallengeCompletionResult { Success = result.Success, Error = result.Error };
        }

        private async Task<ChallengeCompletionResult> SendAsync(string code)
        {
            int generation = _generation;
            try
            {
                var (progress, error) = await _challengeService.UpdateChallengeProgressAsync(code, completed: true, progress: 100f);
                if (error != null)
                {
                    Debug.LogWarning($"[ChallengeCompletionService] Completing {code} failed: {error.message}");
                    return new ChallengeCompletionResult { Success = false, Error = error };
                }

                if (generation != _generation)
                {
                    // Logged out while the request was running: do not keep or announce it for the new user
                    return new ChallengeCompletionResult { Success = true };
                }

                _completed.Add(code);
                ContentReward reward = HasReward(progress?.reward) ? progress.reward : null;
                if (reward != null)
                {
                    _ = _authService?.SyncGamificationAsync();
                }

                ChallengeCompleted?.Invoke(code, reward);
                return new ChallengeCompletionResult { Success = true, Reward = reward };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ChallengeCompletionService] Completing {code} threw: {ex.Message}");
                return new ChallengeCompletionResult { Success = false, Error = new ApiErrorResponse { message = ex.Message } };
            }
            finally
            {
                if (generation == _generation)
                {
                    _inFlight.Remove(code);
                }
            }
        }

        private static bool HasReward(ContentReward reward)
        {
            return reward != null &&
                   ((reward.xp.HasValue && reward.xp.Value > 0) ||
                    (reward.points.HasValue && reward.points.Value > 0) ||
                    !string.IsNullOrEmpty(reward.badgeId));
        }
    }
}
