using System;
using System.Threading.Tasks;

using Unity.AppUI.Core;
using Unity.AppUI.UI;

using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace eu.foodmission.platform
{
    /// <summary>
    /// App UI language context that resolves Smart entries without arguments. App UI 2.1.6 returns the raw
    /// "@UI:KEY" for those (LocalizationUtils.GetLocalizedStringFromLocalizationPackage), but texts using only global
    /// variables such as {global.PET_NAME} need no arguments.
    /// </summary>
    public static class SmartLangContext
    {
        private static Panel s_Panel;

        public static LangContext Create(string lang) => new LangContext(lang) { GetLocalizedStringAsyncFunc = ResolveAsync };

        public static async Task<string> ResolveAsync(string referenceText, string lang, params object[] arguments)
        {
            if (!LocalizationUtils.TryGetTableAndEntry(referenceText, out string table, out string entryKey) || string.IsNullOrEmpty(lang))
            {
                return referenceText;
            }

            try
            {
                LocalizationSettings settings = LocalizationSettings.GetInstanceDontCreateDefault();
                if (settings == null)
                {
                    return referenceText;
                }
                await settings.GetInitializationOperation().Task;

                Locale locale = settings.GetAvailableLocales().GetLocale(lang);
                if (locale == null)
                {
                    return referenceText;
                }
                LocalizedStringDatabase database = settings.GetStringDatabase();
                var stringTable = await database.GetTableAsync(table, locale).Task;
                if (stringTable == null || stringTable.GetEntry(entryKey) == null)
                {
                    return referenceText;
                }
                // Through the database, not the entry: global variables live in the database's Smart formatter.
                // The table is loaded above, so the synchronous call doesn't block
                string text = database.GetLocalizedString(table, entryKey, arguments: arguments ?? Array.Empty<object>(), locale: locale);
                return text ?? referenceText;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SmartLangContext] Could not resolve {referenceText}: {ex.Message}");
                return referenceText;
            }
        }

        /// <summary>
        /// Provides the context on the root panel now and after every locale change. App UI's own listener provides a
        /// plain LangContext on the same panel, so ours is applied on the next frame to win.
        /// </summary>
        public static void Attach(Panel panel)
        {
            Detach();
            if (panel == null)
            {
                return;
            }
            s_Panel = panel;
            LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged;
            Provide(LocalizationSettings.SelectedLocale);
        }

        public static void Detach()
        {
            LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;
            s_Panel = null;
        }

        private static void OnSelectedLocaleChanged(Locale locale) => Provide(locale);

        private static void Provide(Locale locale)
        {
            Panel panel = s_Panel;
            string lang = locale != null ? locale.Identifier.Code : null;
            if (panel == null || string.IsNullOrEmpty(lang))
            {
                return;
            }
            panel.schedule.Execute(() =>
            {
                if (s_Panel == panel)
                {
                    panel.ProvideContext(Create(lang));
                }
            });
        }
    }
}
