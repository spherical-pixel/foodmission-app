using Unity.AppUI.MVVM;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.UIElements;

namespace eu.foodmission.platform.Components
{
    public static class FMLoadingOverlay
    {
        private static VisualElement s_CurrentOverlay;

        public static void Show(string message = null)
        {
            if (s_CurrentOverlay != null && s_CurrentOverlay.parent != null)
            {
                return;
            }

            var root = App.current?.rootVisualElement;
            VisualElement targetContainer = root?.Q<Unity.AppUI.UI.Panel>() ?? root;

            if (targetContainer == null)
            {
                Debug.LogWarning("[FMLoadingOverlay] Cannot find root visual element or AppUI Panel to attach loading overlay.");
                return;
            }

            var overlay = new VisualElement();
            overlay.name = "fm-loading-overlay";
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

            var spinner = new CircularProgress();
            spinner.style.width = 128;
            spinner.style.height = 128;
            overlay.Add(spinner);

            if (!string.IsNullOrEmpty(message))
            {
                var label = new Text { text = message, size = TextSize.L };
                label.style.marginTop = 32;
                label.style.color = Color.white;
                label.style.fontSize = 28;
                label.style.unityFontStyleAndWeight = FontStyle.Bold;

                overlay.Add(label);
            }

            targetContainer.Add(overlay);
            s_CurrentOverlay = overlay;

            NotifyLayoutChanged();
        }

        public static void Hide()
        {
            if (s_CurrentOverlay != null)
            {
                if (s_CurrentOverlay.parent != null)
                {
                    s_CurrentOverlay.parent.Remove(s_CurrentOverlay);
                }
                s_CurrentOverlay = null;
            }

            NotifyLayoutChanged();
        }

        private static void NotifyLayoutChanged()
        {
            if (!AssistiveSupport.isScreenReaderEnabled) return;
            AssistiveSupport.notificationDispatcher?.SendLayoutChanged();
        }
    }
}
