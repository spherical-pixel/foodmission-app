using System;
using UnityEngine.Accessibility;

namespace eu.foodmission.platform
{
    public class AccessibilityService : IAccessibilityService, IDisposable
    {
        public bool IsScreenReaderEnabled => AssistiveSupport.isScreenReaderEnabled;

        public event Action<bool> ScreenReaderStatusChanged;

        public float FontScale => AccessibilitySettings.fontScale;

        public event Action<float> FontScaleChanged;

        public bool IsBoldTextEnabled => AccessibilitySettings.isBoldTextEnabled;

        public event Action<bool> BoldTextStatusChanged;

        public bool IsClosedCaptioningEnabled => AccessibilitySettings.isClosedCaptioningEnabled;

        public event Action<bool> ClosedCaptioningStatusChanged;

        public AccessibilityService()
        {
            AssistiveSupport.screenReaderStatusChanged += OnScreenReaderStatusChanged;
            AccessibilitySettings.fontScaleChanged += OnFontScaleChanged;
            AccessibilitySettings.boldTextStatusChanged += OnBoldTextStatusChanged;
            AccessibilitySettings.closedCaptioningStatusChanged += OnClosedCaptioningStatusChanged;
        }

        public AccessibilityHierarchy CreateHierarchy()
        {
            return new AccessibilityHierarchy();
        }

        public void Dispose()
        {
            AssistiveSupport.screenReaderStatusChanged -= OnScreenReaderStatusChanged;
            AccessibilitySettings.fontScaleChanged -= OnFontScaleChanged;
            AccessibilitySettings.boldTextStatusChanged -= OnBoldTextStatusChanged;
            AccessibilitySettings.closedCaptioningStatusChanged -= OnClosedCaptioningStatusChanged;
        }

        private void OnScreenReaderStatusChanged(bool enabled)
        {
            ScreenReaderStatusChanged?.Invoke(enabled);
        }

        private void OnFontScaleChanged(float scale)
        {
            FontScaleChanged?.Invoke(scale);
        }

        private void OnBoldTextStatusChanged(bool enabled)
        {
            BoldTextStatusChanged?.Invoke(enabled);
        }

        private void OnClosedCaptioningStatusChanged(bool enabled)
        {
            ClosedCaptioningStatusChanged?.Invoke(enabled);
        }
    }
}
