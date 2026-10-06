using System;

using Unity.AppUI.UI;

using UnityEngine.Localization.Settings;

namespace eu.foodmission.platform.Components
{
    /// <summary>Nutri dialog shown when tapping content above the user's level in its dimension.</summary>
    public static class LevelLockedDialog
    {
        public static void Show(LevelLock levelLock, Action onChangeLevels)
        {
            if (levelLock == null)
            {
                return;
            }

            var db = LocalizationSettings.StringDatabase;
            string itemLevel = db.GetLocalizedString("UI", "SEGMENT_" + levelLock.ItemLevel);
            string userLevel = db.GetLocalizedString("UI", "SEGMENT_" + levelLock.UserLevel);
            string dimension = !string.IsNullOrEmpty(levelLock.DimensionName) ? levelLock.DimensionName : levelLock.DimensionCode;
            string message = db.GetLocalizedString("UI", "LEVEL_LOCKED_MSG", new object[] { itemLevel, dimension, userLevel });

            NutriMessageDialog.Show(message,
                new FMDialogAction("@UI:LEVEL_LOCKED_CHANGE", onChangeLevels, ButtonVariant.Accent),
                new FMDialogAction("@UI:LEVEL_LOCKED_CLOSE", null, ButtonVariant.Default));
        }
    }
}
