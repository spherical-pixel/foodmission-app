using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.AppUI.UI;
using eu.foodmission.platform.Components;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class FMLoadingOverlayTests
    {
        private VisualElement _rootContainer;

        [SetUp]
        public void SetUp()
        {
            FMLoadingOverlay.ResetForTesting();
            _rootContainer = new VisualElement();
        }

        [TearDown]
        public void TearDown()
        {
            FMLoadingOverlay.ResetForTesting();
            _rootContainer = null;
        }

        [Test]
        public void PreloadSprites_PopulatesCachedSprites()
        {
            var dummySprite = Sprite.Create(new Texture2D(10, 10), new Rect(0, 0, 10, 10), Vector2.zero);
            var sprites = new[] { dummySprite };

            FMLoadingOverlay.PreloadSprites(sprites);

            Assert.IsNotNull(FMLoadingOverlay.CachedSprites);
            Assert.AreEqual(1, FMLoadingOverlay.CachedSprites.Length);
            Assert.AreSame(dummySprite, FMLoadingOverlay.CachedSprites[0]);
        }

        [Test]
        public void Show_WithAnchor_BuildsCorrectSplashScreenHierarchy()
        {
            var dummySprite = Sprite.Create(new Texture2D(10, 10), new Rect(0, 0, 10, 10), Vector2.zero);
            FMLoadingOverlay.PreloadSprites(new[] { dummySprite });

            FMLoadingOverlay.Show("Custom Loading Message", _rootContainer);

            var overlay = FMLoadingOverlay.CurrentOverlay;
            Assert.IsNotNull(overlay);
            Assert.AreEqual("fm-loading-overlay", overlay.name);
            Assert.IsTrue(overlay.ClassListContains("fm-loading-overlay"));
            Assert.AreSame(_rootContainer, overlay.parent);

            // Container matching SplashScreen
            var loadingContainer = overlay.Q<VisualElement>("loading");
            Assert.IsNotNull(loadingContainer);
            Assert.IsTrue(loadingContainer.ClassListContains("loading-container"));

            // Heading matching SplashScreen: <appui:Heading name="txtLoading" text="Loading" primary="true" size="M" class="centered-text">
            var heading = loadingContainer.Q<Heading>("txtLoading");
            Assert.IsNotNull(heading);
            Assert.AreEqual("Custom Loading Message", heading.text);
            Assert.AreEqual(HeadingSize.M, heading.size);
            Assert.IsTrue(heading.primary);
            Assert.IsTrue(heading.ClassListContains("centered-text"));
            Assert.AreSame(heading, FMLoadingOverlay.CurrentHeading);

            // UISpriteAnimation matching SplashScreen
            var anim = loadingContainer.Q<UISpriteAnimation>("loading-animation");
            Assert.IsNotNull(anim);
            Assert.IsTrue(anim.ClassListContains("loading-animation"));
            Assert.AreEqual(10, anim.style.marginTop.value.value);
            Assert.IsNotNull(anim.Sprites);
            Assert.AreEqual(1, anim.Sprites.Length);
        }

        [Test]
        public void Show_WhenAlreadyShowing_UpdatesHeadingText()
        {
            FMLoadingOverlay.Show("Initial Step", _rootContainer);
            Assert.AreEqual("Initial Step", FMLoadingOverlay.CurrentHeading?.text);

            FMLoadingOverlay.Show("Next Step", _rootContainer);
            Assert.AreEqual("Next Step", FMLoadingOverlay.CurrentHeading?.text);
        }

        [Test]
        public void Hide_ClearsOverlayAndReferences()
        {
            FMLoadingOverlay.Show("Loading...", _rootContainer);
            Assert.IsNotNull(FMLoadingOverlay.CurrentOverlay);
            Assert.AreEqual(1, _rootContainer.childCount);

            FMLoadingOverlay.Hide();

            Assert.IsNull(FMLoadingOverlay.CurrentOverlay);
            Assert.IsNull(FMLoadingOverlay.CurrentHeading);
            Assert.AreEqual(0, _rootContainer.childCount);
        }

        [Test]
        public void Show_WhenNoSpritesPreloaded_UsesFallbackSpinner()
        {
            FMLoadingOverlay.ResetForTesting();

            FMLoadingOverlay.Show("Loading with fallback", _rootContainer);

            var overlay = FMLoadingOverlay.CurrentOverlay;
            Assert.IsNotNull(overlay);

            var loadingContainer = overlay.Q<VisualElement>("loading");
            Assert.IsNotNull(loadingContainer);

            var heading = loadingContainer.Q<Heading>("txtLoading");
            Assert.IsNotNull(heading);
            Assert.AreEqual("Loading with fallback", heading.text);

            var spinner = loadingContainer.Q<CircularProgress>();
            var anim = loadingContainer.Q<UISpriteAnimation>();

            // If AssetDatabase loaded editor sprites, anim will be present. Otherwise fallback spinner is present.
            Assert.IsTrue(anim != null || spinner != null);
        }

        [Test]
        public void HideFor_Owner_HidesOverlay()
        {
            var owner = new object();
            FMLoadingOverlay.Show("Loading", _rootContainer, owner);

            FMLoadingOverlay.HideFor(owner);

            Assert.IsNull(FMLoadingOverlay.CurrentOverlay);
            Assert.AreEqual(0, _rootContainer.childCount);
        }

        [Test]
        public void HideFor_OtherOwner_KeepsOverlay()
        {
            var previousScreen = new object();
            var nextScreen = new object();
            FMLoadingOverlay.Show("Loading", _rootContainer, nextScreen);

            // The screen being left must not hide the overlay opened by the next screen.
            FMLoadingOverlay.HideFor(previousScreen);

            Assert.IsNotNull(FMLoadingOverlay.CurrentOverlay);
        }

        [Test]
        public void HideFor_WhenShownWithoutOwner_KeepsOverlay()
        {
            FMLoadingOverlay.Show("Loading", _rootContainer);

            FMLoadingOverlay.HideFor(new object());

            Assert.IsNotNull(FMLoadingOverlay.CurrentOverlay);
        }

        [Test]
        public void Show_WhenAlreadyShowing_TransfersOwnership()
        {
            var first = new object();
            var second = new object();
            FMLoadingOverlay.Show("First", _rootContainer, first);
            FMLoadingOverlay.Show("Second", _rootContainer, second);

            FMLoadingOverlay.HideFor(first);
            Assert.IsNotNull(FMLoadingOverlay.CurrentOverlay);

            FMLoadingOverlay.HideFor(second);
            Assert.IsNull(FMLoadingOverlay.CurrentOverlay);
        }
    }
}
