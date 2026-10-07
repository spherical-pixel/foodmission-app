using System.Threading.Tasks;

namespace eu.foodmission.platform
{
    /// <summary>Last day of the current quest's missions: Home reminder (Nutri) and local notifications.</summary>
    public interface IMissionLastDayService
    {
        /// <summary>Missions whose last day is today, or null (none, or already told today). Never throws.</summary>
        Task<MissionDeadlines.LastDay> GetTodayAsync();

        /// <summary>Nutri told the user today: not again until tomorrow.</summary>
        void MarkShown();

        bool ShownToday { get; }

        /// <summary>Schedules one local notification per upcoming last day and cancels the ones that no longer apply. Never throws.</summary>
        Task SyncRemindersAsync();
    }
}
