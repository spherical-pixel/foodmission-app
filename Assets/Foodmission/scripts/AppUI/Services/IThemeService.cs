using Unity.AppUI.UI;
using UnityEngine.UIElements;

namespace eu.foodmission.platform
{
    public interface IThemeService : System.IDisposable
    {
        void Initialize(Panel panel);
        void SetTheme(string theme);
        string currentTheme { get; }
        float safeAreaTop { get; }
        float safeAreaRight { get; }
        float safeAreaBottom { get; }
        float safeAreaLeft { get; }
        void ApplySafeAreaPadding(VisualElement element,bool applyTop,bool applyBottom,bool applyLeft, bool applyRight);
        void ApplySafeAreaMargin(VisualElement element);
    }
}
