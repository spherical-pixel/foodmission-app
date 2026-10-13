using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace eu.foodmission.platform
{
    /// <summary>Loads the sustainability progress wheels into AppState and manages which ones are hidden on Home.</summary>
    public interface IProgressWheelService
    {
        bool IsLoading { get; }
        event Action LoadingChanged;

        /// <summary>Fetches the wheels (always the four, BEGINNER before the survey) and keeps userSegment in step with their profile.</summary>
        Task RefreshAsync();

        /// <summary>Stores the hidden kinds locally and syncs them as settings.hiddenProgressWheels.</summary>
        Task SetHiddenAsync(IReadOnlyCollection<string> hiddenKinds);
    }
}
