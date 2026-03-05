using System;

namespace eu.foodmission.platform
{
    /// <summary>
    /// Interface for ViewModels that can request navigation.
    /// Screens using NavigationScreenBase are automatically subscribed to this event.
    /// </summary>
    public interface INavigationAware
    {
        /// <summary>
        /// Event fired when the ViewModel requests navigation.
        /// </summary>
        event Action<string> NavigationRequested;
    }
}
