using System.Collections.Generic;
using Unity.AppUI.UI;
using UnityEngine.UIElements;

namespace eu.foodmission.platform.Components
{
    [UxmlElement]
    public partial class TrafficLightDots : ExVisualElement
    {
        public TrafficLightDots()
        {
            AddToClassList("fm-fi-traffic");
            style.flexDirection = FlexDirection.Row;
        }

        public void SetData(List<TrafficLight> lights)
        {
            Clear();

            if (lights == null || lights.Count == 0)
            {
                style.display = DisplayStyle.None;
                return;
            }

            style.display = DisplayStyle.Flex;

            foreach (var light in lights)
            {
                var dotContainer = new VisualElement();
                dotContainer.AddToClassList("fm-fi-traffic-dot-container");
                dotContainer.style.flexDirection = FlexDirection.Column;
                dotContainer.style.alignItems = Align.Center;

                var dot = new VisualElement();
                dot.AddToClassList("fm-fi-traffic-dot");
                dot.AddToClassList($"fm-fi-traffic-dot--{light.Level?.ToLower() ?? "unknown"}");
                dotContainer.Add(dot);

                var label = new Text { size = TextSize.S, text = light.Label };
                label.AddToClassList("fm-fi-traffic-dot__label");
                dotContainer.Add(label);

                Add(dotContainer);
            }
        }
    }
}
