using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace eu.foodmission.platform
{
    public enum MissionNudgeKind
    {
        MissingDays,
        StalledMission
    }

    public sealed class MissionNudge
    {
        public MissionNudgeKind Kind { get; }
        /// <summary>MissingDays: past days without anything reported.</summary>
        public int MissingDays { get; }
        /// <summary>StalledMission: the mission; null for MissingDays.</summary>
        public string MissionCode { get; }
        public string MissionTitle { get; }
        /// <summary>StalledMission: first automatic module of the mission, null when it has none.</summary>
        public MissionModuleLink AutoModule { get; }
        /// <summary>MissingDays: past days the named mission is waiting for, oldest first (empty when no mission is named).</summary>
        public IReadOnlyList<DateTime> MissionDays { get; }
        /// <summary>MissingDays: other missions are waiting for past days too.</summary>
        public bool OthersWaiting { get; }

        private MissionNudge(MissionNudgeKind kind, int missingDays, string missionCode, string missionTitle, MissionModuleLink autoModule,
            IReadOnlyList<DateTime> missionDays = null, bool othersWaiting = false)
        {
            Kind = kind;
            MissingDays = missingDays;
            MissionCode = missionCode;
            MissionTitle = missionTitle;
            AutoModule = autoModule;
            MissionDays = missionDays ?? Array.Empty<DateTime>();
            OthersWaiting = othersWaiting;
        }

        /// <param name="missionTitle">Mission named in the message (soonest deadline); null = only the day count.</param>
        public static MissionNudge ForMissingDays(int days, string missionTitle = null, IReadOnlyList<DateTime> missionDays = null, bool othersWaiting = false) =>
            new MissionNudge(MissionNudgeKind.MissingDays, days, null, missionTitle, null, missionTitle != null ? missionDays : null, othersWaiting);

        public static MissionNudge ForStalledMission(string missionCode, string missionTitle, MissionModuleLink autoModule) =>
            new MissionNudge(MissionNudgeKind.StalledMission, 0, missionCode, missionTitle, autoModule);
    }

    public interface IMissionNudgeService
    {
        /// <summary>
        /// Missing-days or stalled-mission nudge for Home, or null. Never throws.
        /// Does not start the cooldown: call MarkShown once shown.
        /// </summary>
        Task<MissionNudge> GetNudgeAsync();

        /// <summary>The nudge was shown: starts its 24 h cooldown. Call only once it is on screen.</summary>
        void MarkShown(MissionNudge nudge);

        /// <summary>Forgets the current user's nudge state. Call on logout.</summary>
        void Reset();
    }
}
