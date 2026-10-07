using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using UnityEngine;

namespace eu.foodmission.platform
{
    /// <summary>
    /// Runs the Home prompts of one visit: every blocking prompt, then every informational one, then calls to action
    /// one by one until one leaves Home. Stops as soon as the host is no longer active, before showing anything,
    /// so a prompt that was checked but not shown stays pending (checks never mark anything as seen).
    /// </summary>
    public sealed class HomePromptCoordinator
    {
        public static readonly TimeSpan DefaultGateTimeout = TimeSpan.FromSeconds(60);
        /// <summary>Pause between two shown prompts, so a dialog never opens inside the previous one's close.</summary>
        public static readonly TimeSpan DefaultPromptGap = TimeSpan.FromMilliseconds(250);

        private readonly IReadOnlyList<IHomePrompt> _prompts;
        private readonly ICelebrationGate _gate;
        private readonly TimeSpan _gateTimeout;
        private readonly TimeSpan _promptGap;

        public HomePromptCoordinator(IReadOnlyList<IHomePrompt> prompts, ICelebrationGate gate, TimeSpan? gateTimeout = null, TimeSpan? promptGap = null)
        {
            // OrderBy is stable: within a phase the list order is kept
            _prompts = (prompts ?? Array.Empty<IHomePrompt>()).Where(p => p != null).OrderBy(p => p.Kind).ToList();
            _gate = gate;
            _gateTimeout = gateTimeout ?? DefaultGateTimeout;
            _promptGap = promptGap ?? DefaultPromptGap;
        }

        public async Task RunAsync(IHomePromptHost host)
        {
            bool shownBefore = false;
            foreach (IHomePrompt prompt in _prompts)
            {
                if (host == null || !host.IsActive)
                {
                    return;
                }

                IHomePromptInstance instance = await CheckAsync(prompt);
                if (instance == null)
                {
                    continue;
                }

                if (shownBefore)
                {
                    await Task.Delay(_promptGap);
                }
                await WaitForCelebrationsAsync();
                if (!host.IsActive)
                {
                    return;
                }

                shownBefore = true;
                if (await ShowAsync(instance, host) == HomePromptResult.Navigated)
                {
                    return;
                }
            }
        }

        private static async Task<IHomePromptInstance> CheckAsync(IHomePrompt prompt)
        {
            try
            {
                return await prompt.CheckAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HomePromptCoordinator] {prompt.GetType().Name} check failed: {ex.Message}");
                return null;
            }
        }

        private static async Task<HomePromptResult> ShowAsync(IHomePromptInstance instance, IHomePromptHost host)
        {
            try
            {
                return await instance.ShowAsync(host);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HomePromptCoordinator] {instance.GetType().Name} show failed: {ex.Message}");
                return HomePromptResult.Dismissed;
            }
        }

        private async Task WaitForCelebrationsAsync()
        {
            if (_gate == null || _gate.IsIdle)
            {
                return;
            }

            var idle = new TaskCompletionSource<bool>();
            void OnIdle() => idle.TrySetResult(true);
            _gate.Idle += OnIdle;
            try
            {
                if (_gate.IsIdle)
                {
                    return;
                }
                await Task.WhenAny(idle.Task, Task.Delay(_gateTimeout));
            }
            finally
            {
                _gate.Idle -= OnIdle;
            }
        }
    }
}
