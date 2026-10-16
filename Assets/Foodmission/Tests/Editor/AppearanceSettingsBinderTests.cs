using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using UnityEngine.UIElements;
using eu.foodmission.platform.Components;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class AppearanceSettingsBinderTests
    {
        private Mock<IAuthService> _mockAuthService;
        private TestStoreService _storeService;
        private SettingsViewModel _vm;
        private VisualElement _panel;
        private AppearanceSettingsBinder _binder;

        [SetUp]
        public void SetUp()
        {
            _mockAuthService = new Mock<IAuthService>();
            _mockAuthService.Setup(a => a.SyncSettingsAsync()).Returns(Task.CompletedTask);
            _storeService = new TestStoreService();
            AppState state = _storeService.GetAppState();
            state.theme = "dark";
            state.lang = "es";
            state.scale = "large";
            state.font = "open-dyslexic";
            state.backgroundPattern = false;
            _storeService.SetAppState(state);
            _vm = new SettingsViewModel(_storeService, _mockAuthService.Object);
            _panel = AppearanceSettingsDialog.BuildPanel(out _binder);
        }

        [TearDown]
        public void TearDown()
        {
            _binder?.Unbind();
            _vm?.Dispose();
            _storeService?.Dispose();
        }

        private FormFieldItemArrowStepperSettings Stepper(string name) =>
            _panel.Q<FormFieldItemArrowStepperSettings>(name);

        [Test]
        public void BuildPanel_HasTheFiveAppearanceSteppersWithLocalizedHeadings()
        {
            Assert.AreEqual("@UI:LANGUAGE", Stepper("stepper-lang").HeadingText);
            Assert.AreEqual("@UI:TEXT_SIZE", Stepper("stepper-scale").HeadingText);
            Assert.AreEqual("@UI:FONT", Stepper("stepper-font").HeadingText);
            Assert.AreEqual("@UI:THEME", Stepper("stepper-theme").HeadingText);
            Assert.AreEqual("@UI:BACKGROUND", Stepper("stepper-background").HeadingText);
            Assert.AreEqual(5, _panel.Query<FormFieldItemArrowStepperSettings>().ToList().Count);
        }

        [Test]
        public void Bind_ShowsCurrentValuesWithoutDispatching()
        {
            int before = _storeService.DispatchedActionTypes.Count;

            _binder.Bind(_vm);

            Assert.AreEqual(8, Stepper("stepper-lang").SelectedIndex);
            Assert.AreEqual(2, Stepper("stepper-scale").SelectedIndex);
            Assert.AreEqual(2, Stepper("stepper-font").SelectedIndex);
            Assert.AreEqual(1, Stepper("stepper-theme").SelectedIndex);
            Assert.AreEqual(0, Stepper("stepper-background").SelectedIndex);
            Assert.AreEqual(before, _storeService.DispatchedActionTypes.Count);
        }

        [Test]
        public void ChangingSteppers_UpdatesAppState()
        {
            _binder.Bind(_vm);

            Stepper("stepper-lang").SelectedIndex = 1;
            Stepper("stepper-scale").SelectedIndex = 0;
            Stepper("stepper-font").SelectedIndex = 1;
            Stepper("stepper-theme").SelectedIndex = 0;
            Stepper("stepper-background").SelectedIndex = 1;

            AppState state = _storeService.GetAppState();
            Assert.AreEqual("en", state.lang);
            Assert.AreEqual("small", state.scale);
            Assert.AreEqual("open-sans", state.font);
            Assert.AreEqual("light", state.theme);
            Assert.IsTrue(state.backgroundPattern);
        }

        [Test]
        public void Unbind_StopsDispatching()
        {
            _binder.Bind(_vm);
            _binder.Unbind();
            int before = _storeService.DispatchedActionTypes.Count;

            Stepper("stepper-theme").SelectedIndex = 0;

            Assert.AreEqual(before, _storeService.DispatchedActionTypes.Count);
            Assert.AreEqual("dark", _storeService.GetAppState().theme);
        }

        [Test]
        public void NullSteppers_AreIgnored()
        {
            var binder = new AppearanceSettingsBinder(null, null, null, null, null);

            Assert.DoesNotThrow(() =>
            {
                binder.Bind(_vm);
                binder.CreateAccessibilityNodes(null);
                binder.DestroyAccessibilityNodes();
                binder.Unbind();
            });
        }

        [TestCase("light", 0)]
        [TestCase("dark", 1)]
        [TestCase("system", 2)]
        [TestCase("", 2)]
        public void Theme_RoundTrips(string theme, int index)
        {
            Assert.AreEqual(index, AppearanceSettingsBinder.ThemeToIndex(theme));
            if (!string.IsNullOrEmpty(theme))
            {
                Assert.AreEqual(theme, AppearanceSettingsBinder.IndexToTheme(index));
            }
        }

        [TestCase("nl", 0)]
        [TestCase("en", 1)]
        [TestCase("sl", 7)]
        [TestCase("es", 8)]
        public void Lang_RoundTrips(string lang, int index)
        {
            Assert.AreEqual(index, AppearanceSettingsBinder.LangToIndex(lang));
            Assert.AreEqual(lang, AppearanceSettingsBinder.IndexToLang(index));
        }

        [Test]
        public void Lang_UnknownOrNoneFallsBackToEnglish()
        {
            Assert.AreEqual(1, AppearanceSettingsBinder.LangToIndex("none"));
            Assert.AreEqual(1, AppearanceSettingsBinder.LangToIndex(null));
        }

        [TestCase("small", 0)]
        [TestCase("medium", 1)]
        [TestCase("large", 2)]
        public void Scale_RoundTrips(string scale, int index)
        {
            Assert.AreEqual(index, AppearanceSettingsBinder.ScaleToIndex(scale));
            Assert.AreEqual(scale, AppearanceSettingsBinder.IndexToScale(index));
        }

        [TestCase("roboto", 0)]
        [TestCase("open-sans", 1)]
        [TestCase("open-dyslexic", 2)]
        public void Font_RoundTrips(string font, int index)
        {
            Assert.AreEqual(index, AppearanceSettingsBinder.FontToIndex(font));
            Assert.AreEqual(font, AppearanceSettingsBinder.IndexToFont(index));
        }

        [Test]
        public void ChoiceLists_MatchTheMappings()
        {
            Assert.AreEqual(9, AppearanceSettingsBinder.LangChoices.Length);
            Assert.AreEqual(3, AppearanceSettingsBinder.ThemeChoices.Length);
            Assert.AreEqual(3, AppearanceSettingsBinder.ScaleChoices.Length);
            Assert.AreEqual(3, AppearanceSettingsBinder.FontChoices.Length);
            Assert.AreEqual(2, AppearanceSettingsBinder.BackgroundChoices.Length);
        }

        [Test]
        public void SyncSettingsAsync_WithoutSession_ReturnsBeforeAnyRequest()
        {
            using var storeService = new TestStoreService();
            var authService = new AuthService(storeService);

            Task sync = authService.SyncSettingsAsync();

            // Settings changed on Login / Register stay local until there is a session
            Assert.IsTrue(sync.IsCompleted);
            (authService as System.IDisposable)?.Dispose();
        }
    }
}
