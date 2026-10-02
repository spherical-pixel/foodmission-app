using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

namespace eu.foodmission.platform.Components
{
    /// <summary>
    /// Five stars built from the tinted fm-star image (styles in FMStarRating.uss). Read-only it shows a fractional
    /// value (4.2 → last star 20 % filled); with <see cref="interactive"/> tapping star N raises <see cref="StarClicked"/>(N).
    /// </summary>
    [UxmlElement]
    public partial class FMStarRating : VisualElement
    {
        public const string UssClassName = "fm-star-rating";
        public const string InteractiveUssClassName = "fm-star-rating--interactive";
        public const string StarUssClassName = "fm-star-rating__star";
        public const string EmptyUssClassName = "fm-star-rating__empty";
        public const string FillUssClassName = "fm-star-rating__fill";
        public const string FillImageUssClassName = "fm-star-rating__fill-image";
        public const int StarCount = 5;

        private readonly List<VisualElement> _stars = new List<VisualElement>();
        private readonly List<VisualElement> _fills = new List<VisualElement>();
        private float _value;
        private bool _interactive;

        public event Action<int> StarClicked;

        public IReadOnlyList<VisualElement> Stars => _stars;

        [UxmlAttribute]
        public float value
        {
            get => _value;
            set
            {
                _value = Mathf.Clamp(value, 0f, StarCount);
                Refresh();
            }
        }

        [UxmlAttribute]
        public bool interactive
        {
            get => _interactive;
            set
            {
                _interactive = value;
                EnableInClassList(InteractiveUssClassName, value);
            }
        }

        public FMStarRating()
        {
            AddToClassList(UssClassName);
            for (int i = 0; i < StarCount; i++)
            {
                int stars = i + 1;
                var star = new VisualElement();
                star.AddToClassList(StarUssClassName);

                var empty = new VisualElement();
                empty.AddToClassList(EmptyUssClassName);
                star.Add(empty);

                // Clipped overlay: its width is the filled fraction, the image inside keeps the full star size.
                var fill = new VisualElement();
                fill.AddToClassList(FillUssClassName);
                var fillImage = new VisualElement();
                fillImage.AddToClassList(FillImageUssClassName);
                fill.Add(fillImage);
                star.Add(fill);

                star.AddManipulator(new Clickable(() => Select(stars)));
                _stars.Add(star);
                _fills.Add(fill);
                Add(star);
            }
            Refresh();
        }

        /// <summary>Called when star <paramref name="stars"/> (1–5) is tapped; ignored when not interactive.</summary>
        public void Select(int stars)
        {
            if (!_interactive || stars < 1 || stars > StarCount)
            {
                return;
            }
            StarClicked?.Invoke(stars);
        }

        public static float FillFraction(float value, int index)
        {
            return Mathf.Clamp01(value - index);
        }

        private void Refresh()
        {
            for (int i = 0; i < _fills.Count; i++)
            {
                _fills[i].style.width = Length.Percent(FillFraction(_value, i) * 100f);
            }
        }
    }
}
