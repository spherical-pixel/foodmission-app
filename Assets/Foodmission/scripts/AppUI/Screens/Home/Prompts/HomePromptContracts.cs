using System;
using System.Threading.Tasks;

using eu.foodmission.platform.Components;

using UnityEngine.UIElements;

namespace eu.foodmission.platform
{
    /// <summary>Phase of a Home prompt: blocking first, then every informational one, then calls to action.</summary>
    public enum HomePromptKind
    {
        Blocking,
        Info,
        Action
    }

    public enum HomePromptResult
    {
        /// <summary>Closed without leaving Home: the run goes on.</summary>
        Dismissed,
        /// <summary>Left Home, or a blocking prompt was not resolved: the run stops.</summary>
        Navigated
    }

    public interface IHomePrompt
    {
        HomePromptKind Kind { get; }

        /// <summary>Only queries; never persists "seen". Null when nothing is pending.</summary>
        Task<IHomePromptInstance> CheckAsync();
    }

    public interface IHomePromptInstance
    {
        /// <summary>Shows the prompt, marks it as seen when shown, and resolves once when it closes by any path.</summary>
        Task<HomePromptResult> ShowAsync(IHomePromptHost host);
    }

    public interface IHomePromptHost
    {
        /// <summary>Home is still bound and this is still the same visit.</summary>
        bool IsActive { get; }

        VisualElement DialogAnchor { get; }

        void RefreshActiveQuestWidget();

        void NavigateToAuth();
    }

    public interface ICelebrationGate
    {
        bool IsIdle { get; }

        event Action Idle;
    }

    /// <summary>The global reward celebration queue, as seen by the Home prompt coordinator.</summary>
    public sealed class RewardCelebrationGate : ICelebrationGate
    {
        public bool IsIdle => RewardCelebrationDialog.IsIdle;

        public event Action Idle
        {
            add => RewardCelebrationDialog.Idle += value;
            remove => RewardCelebrationDialog.Idle -= value;
        }
    }

    /// <summary>State shared by the prompts of one Home visit.</summary>
    public sealed class HomePromptRunState
    {
        /// <summary>Set by the reward celebrations when a quest was completed; opened by UnlockedQuestPrompt.</summary>
        public Quest QuestToOffer;
    }
}
