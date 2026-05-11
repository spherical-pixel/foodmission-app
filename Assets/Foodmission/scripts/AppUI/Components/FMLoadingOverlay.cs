using System.Collections.Generic;

using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace eu.foodmission.platform.Components
{
    public static class FMLoadingOverlay
    {
        private static readonly Dictionary<VisualElement, VisualElement> s_Overlays = new();

        public static void Show(VisualElement anchor, string message = null)
        {
            if (anchor == null) return;

            if (s_Overlays.TryGetValue(anchor, out VisualElement existing))
            {
                if (existing.parent != null) return;
                s_Overlays.Remove(anchor);
            }

            var overlay = new VisualElement();
            overlay.style.position = Position.Absolute;
            overlay.style.top = 0;
            overlay.style.bottom = 0;
            overlay.style.left = 0;
            overlay.style.right = 0;
            overlay.style.justifyContent = Justify.Center;
            overlay.style.alignItems = Align.Center;
            overlay.style.backgroundColor = new StyleColor(new Color(0, 0, 0, 0.47f));
            overlay.pickingMode = PickingMode.Ignore;

            var spinner = new CircularProgress();
            spinner.style.width = 48;
            spinner.style.height = 48;
            overlay.Add(spinner);

            if (!string.IsNullOrEmpty(message))
            {
                var label = new Text { text = message };
                label.style.marginTop = 12;
                label.style.color = Color.white;
                label.style.fontSize = 16;
                overlay.Add(label);
            }

            anchor.Add(overlay);
            s_Overlays[anchor] = overlay;
        }

        public static void Hide(VisualElement anchor)
        {
            if (anchor == null) return;

            if (s_Overlays.TryGetValue(anchor, out VisualElement overlay))
            {
                if (overlay.parent != null)
                {
                    anchor.Remove(overlay);
                }

                s_Overlays.Remove(anchor);
            }
        }

        public static void HideAll()
        {
            foreach (KeyValuePair<VisualElement, VisualElement> kvp in s_Overlays)
            {
                if (kvp.Value.parent != null)
                {
                    kvp.Key.Remove(kvp.Value);
                }
            }

            s_Overlays.Clear();
        }
    }
}
