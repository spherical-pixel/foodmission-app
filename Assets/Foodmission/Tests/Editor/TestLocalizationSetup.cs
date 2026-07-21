using NUnit.Framework;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace eu.foodmission.platform.Tests
{
    [SetUpFixture]
    public class TestLocalizationSetup
    {
        private LocalizationSettings _previousSettings;

        [OneTimeSetUp]
        public void SetupLocalization()
        {
            _previousSettings = LocalizationSettings.GetInstanceDontCreateDefault();

            var testSettings = ScriptableObject.CreateInstance<LocalizationSettings>();
            var locale = Locale.CreateLocale("en");

            var localesProvider = new LocalesProvider();
            localesProvider.AddLocale(locale);
            testSettings.SetAvailableLocales(localesProvider);

            LocalizationSettings.Instance = testSettings;
            LocalizationSettings.SelectedLocale = locale;
        }

        [OneTimeTearDown]
        public void TearDownLocalization()
        {
            LocalizationSettings.Instance = _previousSettings;
        }
    }
}
