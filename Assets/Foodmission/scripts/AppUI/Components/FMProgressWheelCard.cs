using System;

using Unity.AppUI.UI;

using UnityEngine.UIElements;

namespace eu.foodmission.platform.Components
{
    /// <summary>One sustainability progress wheel: ring with percent plus every value the backend sends.</summary>
    /// <remarks>Extends App UI ExVisualElement because only it renders the --box-shadow-* USS variables.</remarks>
    [UxmlElement]
    public partial class FMProgressWheelCard : ExVisualElement
    {
        public const string UssClassName = "fm-wheel-card";
        public const string HorizontalUssClassName = "fm-wheel-card--horizontal";
        public const string ClickableUssClassName = "fm-wheel-card--clickable";
        public const string InfoIconUssClassName = "fm-wheel-card__info";
        public const string InfoIconHiddenUssClassName = "fm-wheel-card__info--hidden";

        private readonly CircularProgress _ring;
        private readonly Text _percent;
        private readonly Text _name;
        private readonly Text _stage;
        private readonly Text _stageTitle;
        private readonly Text _value;
        private readonly Text _meta;
        private readonly Icon _infoIcon;
        private string _colorClass;
        private bool _interactive = true;

        public string Kind { get; private set; }
        public string AccessibilityLabel { get; private set; } = "";

        /// <summary>Raised with the wheel kind when the card is tapped (Home opens its explanation).</summary>
        public event Action<string> clicked;

        /// <summary>Whether the host lets the card be tapped; false on the explanation screen itself.</summary>
        public bool Interactive
        {
            get => _interactive;
            set
            {
                _interactive = value;
                RefreshInteractivity();
            }
        }

        /// <summary>True when tapping opens an explanation: the host allows it and the app has a text for this wheel.</summary>
        public bool CanOpen => _interactive && ProgressWheelInfoCatalog.ParagraphCount(Kind) > 0;

        public FMProgressWheelCard()
        {
            AddToClassList(UssClassName);

            var ringWrapper = new VisualElement();
            ringWrapper.AddToClassList("fm-wheel-card__ring-wrapper");
            _ring = new CircularProgress
            {
                variant = Progress.Variant.Determinate,
                bufferValue = 1f,
                innerRadius = 0.40f,
            };
            _ring.AddToClassList("fm-wheel-card__ring");
            _percent = new Text { size = TextSize.L };
            _percent.AddToClassList("fm-wheel-card__percent");
            ringWrapper.Add(_ring);
            ringWrapper.Add(_percent);
            Add(ringWrapper);

            var texts = new VisualElement();
            texts.AddToClassList("fm-wheel-card__texts");
            _name = AddText(texts, "fm-wheel-card__name", TextSize.M);
            _stage = AddText(texts, "fm-wheel-card__stage", TextSize.S);
            _stageTitle = AddText(texts, "fm-wheel-card__stage-title", TextSize.S);
            _value = AddText(texts, "fm-wheel-card__value", TextSize.S);
            _meta = AddText(texts, "fm-wheel-card__meta", TextSize.S);
            Add(texts);

            _infoIcon = new Icon { iconName = "info" };
            _infoIcon.AddToClassList(InfoIconUssClassName);
            Add(_infoIcon);

            RefreshInteractivity();
            this.AddManipulator(new UnityEngine.UIElements.Clickable(Activate));
        }

        /// <summary>Same as a tap: raises <see cref="clicked"/> when <see cref="CanOpen"/> (also used by the screen reader action).</summary>
        public void Activate()
        {
            if (CanOpen)
            {
                clicked?.Invoke(Kind);
            }
        }

        private void RefreshInteractivity()
        {
            bool canOpen = CanOpen;
            EnableInClassList(ClickableUssClassName, canOpen);
            _infoIcon.EnableInClassList(InfoIconHiddenUssClassName, !canOpen);
        }

        private static Text AddText(VisualElement parent, string ussClass, TextSize size)
        {
            var text = new Text { size = size };
            text.AddToClassList(ussClass);
            parent.Add(text);
            return text;
        }

        public void Bind(ProgressWheel wheel)
        {
            Kind = wheel?.kind;
            RefreshInteractivity();

            string colorClass = ProgressWheelText.ColorClass(Kind);
            if (_colorClass != colorClass)
            {
                if (_colorClass != null)
                {
                    RemoveFromClassList(_colorClass);
                }
                AddToClassList(colorClass);
                _colorClass = colorClass;
            }

            float progress = ProgressWheelText.ClampPercent(wheel?.percentComplete ?? 0f) / 100f;
            _ring.value = progress;
            // App UI draws the rounded start cap even at 0, which shows as a stray dot on an empty ring.
            _ring.roundedProgressCorners = progress > 0f;

            string name = ProgressWheelText.Name(wheel);
            string percent = ProgressWheelText.PercentText(wheel);
            string stage = ProgressWheelText.StageLine(wheel);
            string stageTitle = ProgressWheelText.StageTitle(wheel);
            string value = ProgressWheelText.ValueLine(wheel);
            string meta = $"{ProgressWheelText.TargetLine(wheel)} · {ProgressWheelText.TotalLine(wheel)}";

            _percent.text = percent;
            _name.text = name;
            _stage.text = stage;
            _stageTitle.text = stageTitle;
            _value.text = value;
            _meta.text = meta;

            AccessibilityLabel = string.Join(", ", name, stage, stageTitle, percent, value, meta);
        }

        public void SetHorizontal(bool horizontal)
        {
            EnableInClassList(HorizontalUssClassName, horizontal);
        }
    }
}
