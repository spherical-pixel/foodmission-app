using System;
using System.Collections.Generic;
using Unity.AppUI.MVVM;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;

namespace eu.foodmission.platform.Components
{
    public static class FMLoadingOverlay
    {
        private static VisualElement s_CurrentOverlay;
        private static Heading s_CurrentHeading;
        private static Sprite[] s_CachedSprites;

        /// <summary>
        /// Preload or inject the sprite animation frames for the loading overlay.
        /// Called automatically during app startup by SplashScreen.
        /// </summary>
        public static void PreloadSprites(Sprite[] sprites)
        {
            if (sprites != null && sprites.Length > 0)
            {
                s_CachedSprites = sprites;
            }
        }

        /// <summary>
        /// Preload the sprite animation frames from a VisualTreeAsset (such as SplashScreen.uxml).
        /// </summary>
        public static void PreloadSprites(VisualTreeAsset splashTemplate = null)
        {
            if (s_CachedSprites != null && s_CachedSprites.Length > 0) return;

            var template = splashTemplate ?? FoodmissionAppBuilder.instance?.SplashTemplate;
#if UNITY_EDITOR
            if (template == null)
            {
                template = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                    "Assets/Foodmission/scripts/AppUI/Screens/Splash/SplashScreen.uxml");
            }
#endif
            if (template != null)
            {
                var temp = template.CloneTree();
                var anim = temp.Q<UISpriteAnimation>();
                if (anim != null && anim.Sprites != null && anim.Sprites.Length > 0)
                {
                    s_CachedSprites = anim.Sprites;
                }
            }
        }

        public static void Show(string message = null, VisualElement anchor = null)
        {
            if (s_CurrentOverlay != null && s_CurrentOverlay.parent != null)
            {
                if (!string.IsNullOrEmpty(message) && s_CurrentHeading != null)
                {
                    s_CurrentHeading.text = message;
                }
                return;
            }

            // Fall back to the anchor itself when it is not attached to a panel yet (and there is no App).
            var root = anchor?.panel?.visualTree ?? App.current?.rootVisualElement ?? anchor;
            VisualElement targetContainer = root?.Q<Unity.AppUI.UI.Panel>() ?? root;

            if (targetContainer == null)
            {
                Debug.LogWarning("[FMLoadingOverlay] Cannot find root visual element or AppUI Panel to attach loading overlay.");
                return;
            }

            var overlay = new VisualElement();
            overlay.name = "fm-loading-overlay";
            overlay.AddToClassList("fm-loading-overlay");
            overlay.style.position = Position.Absolute;
            overlay.style.top = 0;
            overlay.style.bottom = 0;
            overlay.style.left = 0;
            overlay.style.right = 0;
            overlay.style.width = Length.Percent(100);
            overlay.style.height = Length.Percent(100);
            overlay.style.justifyContent = Justify.Center;
            overlay.style.alignItems = Align.Center;
            overlay.style.backgroundColor = new StyleColor(new Color(0, 0, 0, 0.47f));
            overlay.pickingMode = PickingMode.Position;

            // Intercept pointer down and click events to prevent user interaction while loading
            overlay.RegisterCallback<PointerDownEvent>(evt => evt.StopPropagation());
            overlay.RegisterCallback<ClickEvent>(evt => evt.StopPropagation());

            // Loading container matching SplashScreen: <ui:VisualElement name="loading" class="loading-container">
            var loadingContainer = new VisualElement();
            loadingContainer.name = "loading";
            loadingContainer.AddToClassList("loading-container");
            loadingContainer.style.justifyContent = Justify.Center;
            loadingContainer.style.alignItems = Align.Center;
            loadingContainer.style.paddingLeft = 24;
            loadingContainer.style.paddingRight = 24;

            // Heading matching SplashScreen: <appui:Heading name="txtLoading" text="Loading" primary="true" size="M" class="centered-text">
            string displayText = !string.IsNullOrEmpty(message) ? message : GetDefaultLoadingText();
            var heading = new Heading
            {
                name = "txtLoading",
                text = displayText,
                primary = true,
                size = HeadingSize.M
            };
            heading.AddToClassList("centered-text");
            heading.style.color = Color.white;
            heading.style.unityTextAlign = TextAnchor.MiddleCenter;
            heading.style.alignSelf = Align.Center;
            heading.style.textShadow = new TextShadow
            {
                color = new Color(0f, 0f, 0f, 0.85f),
                offset = new Vector2(0f, 2f),
                blurRadius = 4f
            };
            loadingContainer.Add(heading);
            s_CurrentHeading = heading;

            // Sprite animation matching SplashScreen: <eu.foodmission.platform.Components.UISpriteAnimation ... fps="30" class="loading-animation" style="margin-top: 10px;"/>
            var sprites = GetSprites();
            if (sprites != null && sprites.Length > 0)
            {
                var spriteAnim = new UISpriteAnimation(sprites, 0f, 30, CycleType.Loop);
                spriteAnim.name = "loading-animation";
                spriteAnim.AddToClassList("loading-animation");
                spriteAnim.scaleMode = ScaleMode.ScaleToFit;
                spriteAnim.style.alignSelf = Align.Center;
                spriteAnim.style.marginTop = 10;
                spriteAnim.Sprites = sprites;
                loadingContainer.Add(spriteAnim);
            }
            else
            {
                var spinner = new CircularProgress();
                spinner.style.width = 64;
                spinner.style.height = 64;
                spinner.style.marginTop = 10;
                loadingContainer.Add(spinner);
            }

            overlay.Add(loadingContainer);
            targetContainer.Add(overlay);
            s_CurrentOverlay = overlay;

            NotifyLayoutChanged();
        }

        public static void Hide()
        {
            if (s_CurrentOverlay != null)
            {
                var anim = s_CurrentOverlay.Q<UISpriteAnimation>();
                anim?.Stop();

                if (s_CurrentOverlay.parent != null)
                {
                    s_CurrentOverlay.parent.Remove(s_CurrentOverlay);
                }
                s_CurrentOverlay = null;
                s_CurrentHeading = null;
            }

            NotifyLayoutChanged();
        }

        private static Sprite[] GetSprites()
        {
            if (s_CachedSprites != null && s_CachedSprites.Length > 0)
            {
                return s_CachedSprites;
            }

            PreloadSprites();

            if (s_CachedSprites != null && s_CachedSprites.Length > 0)
            {
                return s_CachedSprites;
            }

#if UNITY_EDITOR
            var list = new List<Sprite>();
            for (int i = 1; i <= 39; i++)
            {
                string path = $"Assets/Foodmission/graphics/png/load-anim/white/load_blanco{i:D4}.png";
                var sp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sp != null)
                {
                    list.Add(sp);
                }
            }
            if (list.Count > 0)
            {
                s_CachedSprites = list.ToArray();
                return s_CachedSprites;
            }
#endif

            return Array.Empty<Sprite>();
        }

        private static string GetDefaultLoadingText()
        {
            try
            {
                if (LocalizationSettings.StringDatabase != null)
                {
                    string localized = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "LOADING");
                    if (!string.IsNullOrEmpty(localized) && localized != "LOADING")
                    {
                        return localized;
                    }
                }
            }
            catch
            {
                // Localization not ready or running in tests
            }

            return "Loading";
        }

        private static void NotifyLayoutChanged()
        {
            if (!AssistiveSupport.isScreenReaderEnabled) return;
            AssistiveSupport.notificationDispatcher?.SendLayoutChanged();
        }

        // Test helpers
        public static VisualElement CurrentOverlay => s_CurrentOverlay;
        public static Heading CurrentHeading => s_CurrentHeading;
        public static Sprite[] CachedSprites => s_CachedSprites;
        public static void ResetForTesting()
        {
            Hide();
            s_CachedSprites = null;
        }
    }
}
