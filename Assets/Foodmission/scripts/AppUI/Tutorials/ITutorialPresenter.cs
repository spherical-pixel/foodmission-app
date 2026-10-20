using System.Collections.Generic;
using System.Threading.Tasks;

namespace eu.foodmission.platform
{
    /// <summary>Shows tutorial bubbles; the seam between <see cref="TutorialService"/> and the UI.</summary>
    public interface ITutorialPresenter
    {
        /// <summary>True while any App UI modal (Nutri bubble, FMDialog, celebration...) is on screen.</summary>
        bool IsAnyDialogOpen { get; }

        /// <summary>
        /// Shows one bubble per key (Next/Skip between them) and resolves once the last one closes by any path.
        /// True when at least the first bubble was shown.
        /// </summary>
        Task<bool> ShowAsync(IReadOnlyList<string> messageKeys);
    }
}
