using UnityEngine;

namespace eu.foodmission.platform
{
    /// <summary>
    /// Where the AppBar compact title goes between the side buttons: centered on the bar when it fits,
    /// otherwise all the free space between the buttons (off-center, like iOS), ellipsized if it still overflows.
    /// </summary>
    public static class AppBarTitleLayout
    {
        public const float Gap = 8f;

        /// <param name="barWidth">Bar width.</param>
        /// <param name="leading">Right edge of the left buttons (0 when none).</param>
        /// <param name="trailing">Width taken by the right buttons, from the bar's right edge (0 when none).</param>
        /// <param name="textWidth">Preferred width of the title text.</param>
        /// <returns>Left and right insets of the title.</returns>
        public static (float Left, float Right) Insets(float barWidth, float leading, float trailing, float textWidth)
        {
            float centered = Mathf.Max(leading, trailing) + Gap;
            if (textWidth <= barWidth - 2f * centered)
            {
                return (centered, centered);
            }
            return (leading + Gap, trailing + Gap);
        }
    }
}
