using System;
using UnityEngine.Accessibility;

namespace eu.foodmission.platform
{
    public interface IAccessibilityService
    {
        bool IsScreenReaderEnabled { get; }
        event Action<bool> ScreenReaderStatusChanged;

        float FontScale { get; }
        event Action<float> FontScaleChanged;

        bool IsBoldTextEnabled { get; }
        event Action<bool> BoldTextStatusChanged;

        bool IsClosedCaptioningEnabled { get; }
        event Action<bool> ClosedCaptioningStatusChanged;

        AccessibilityHierarchy CreateHierarchy();
    }
}
