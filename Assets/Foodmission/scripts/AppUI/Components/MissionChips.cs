using System;

using Unity.AppUI.UI;

using eu.foodmission.platform.Components;

namespace eu.foodmission.platform
{
    /// <summary>Selectable chips of mission answers, shared by Nutri's check-in and the product detail's mission cards.</summary>
    public static class MissionChips
    {
        public static FMButton Chip(string title, bool selected, Action onClick)
        {
            // The UI font has no "✓" glyph: mark selection with App UI's check icon
            var chip = new FMButton
            {
                title = title,
                leadingIcon = selected ? "check" : string.Empty,
                variant = selected ? ButtonVariant.Accent : ButtonVariant.Default,
                size = Size.S
            };
            chip.AddToClassList("fm-mission-report-chip");
            chip.EnableInClassList("fm-mission-report-chip--selected", selected);
            chip.clicked += onClick;
            return chip;
        }
    }
}
