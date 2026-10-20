using System;
using System.Threading.Tasks;

namespace eu.foodmission.platform
{
    /// <summary>Section tutorials told by Nutri: shown once per user (settings.seenTutorials) and on demand from the AppBar.</summary>
    public interface ITutorialService
    {
        bool HasSeen(string id);

        /// <summary>Shows the tutorial even if already seen (AppBar ?, Home prompt) and marks it seen once shown.</summary>
        Task ShowAsync(string id);

        /// <summary>
        /// After a short delay, shows the destination's tutorial if it has one, it was never seen, the user is still on
        /// that destination and no other dialog is open. Home is skipped: it runs in the Home prompt queue.
        /// </summary>
        Task ShowIfFirstTimeAsync(string destinationName, Func<string> currentDestination);
    }
}
