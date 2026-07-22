using System;
using UnityEngine.Localization.Settings;

namespace eu.foodmission.platform
{
    /// <summary>
    /// Represents a localized UI option definition with an optional parameter list for Unity Localization Smart Format.
    /// Example: new LocalizedOption("txt_times_per_week", "1–2") -> "1–2 veces por semana"
    /// </summary>
    [Serializable]
    public readonly struct LocalizedOption
    {
        public readonly string TableName;
        public readonly string Key;
        public readonly object[] Args;


        public LocalizedOption(string tableName, string key, params object[] args)
        {
            TableName = tableName;
            Key = key;
            Args = args;
        }

        /// <summary>
        /// Returns the localized string from Unity Localization StringDatabase with parameters applied.
        /// </summary>
        public string GetText()
        {
            if (string.IsNullOrEmpty(Key)) return "";

            try
            {
                if (Args == null || Args.Length == 0)
                {
                    return LocalizationSettings.StringDatabase.GetLocalizedString(TableName, Key);
                }
                return LocalizationSettings.StringDatabase.GetLocalizedString(TableName, Key, Args);
            }
            catch
            {
                return Key;
            }
        }

        public override string ToString() => GetText();
    }
}
