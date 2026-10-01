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

        private MissionNudge(MissionNudgeKind kind, int missingDays, string missionCode, string missionTitle, MissionModuleLink autoModule)
        {
            Kind = kind;
            MissingDays = missingDays;
            MissionCode = missionCode;
            MissionTitle = missionTitle;
            AutoModule = autoModule;
        }

        public static MissionNudge ForMissingDays(int days) =>
            new MissionNudge(MissionNudgeKind.MissingDays, days, null, null, null);

        public static MissionNudge ForStalledMission(string missionCode, string missionTitle, MissionModuleLink autoModule) =>
            new MissionNudge(MissionNudgeKind.StalledMission, 0, missionCode, missionTitle, autoModule);
    }

    public interface IMissionNudgeService
    {
        /// <summary>Missing-days or stalled-mission nudge for Home, or null. Never throws.</summary>
        Task<MissionNudge> GetNudgeAsync();

        /// <summary>Forgets the current user's nudge state. Call on logout.</summary>
        void Reset();
    }
}
