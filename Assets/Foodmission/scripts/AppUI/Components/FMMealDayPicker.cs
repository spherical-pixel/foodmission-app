using System;
using System.Collections.Generic;
using System.Globalization;

using Unity.AppUI.UI;

using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;

namespace eu.foodmission.platform.Components
{
    /// <summary>
    /// Day a meal is logged for (<see cref="MealLogDate"/>), shared by the full and the simplified meal log.
    /// Chips newest first: Today, Yesterday, then weekday + day. Renders only; the owner listens to
    /// <see cref="DaySelected"/> and sets <see cref="SelectedDay"/>.
    /// </summary>
    [UxmlElement]
    public partial class FMMealDayPicker : VisualElement
    {
        private readonly Text _title;
        private readonly VisualElement _chipsRow;
        private readonly List<(DateTime Day, FMButton Chip)> _chips = new();
        private DateTime _selectedDay;

        public event Action<DateTime> DaySelected;

        public IReadOnlyList<(DateTime Day, FMButton Chip)> Chips => _chips;

        /// <summary>The question above the chips, so each screen can give it its own heading style.</summary>
        public Text Title => _title;

        public DateTime SelectedDay
        {
            get => _selectedDay;
            set
            {
                _selectedDay = value.Date;
                UpdateSelection();
            }
        }

        public FMMealDayPicker()
        {
            AddToClassList("fm-meal-day-picker");

            _title = new Text { text = "@UI:MEAL_LOG_DAY_QUESTION", size = TextSize.M };
            _title.AddToClassList("fm-meal-day-picker-title");
            Add(_title);

            _chipsRow = new VisualElement();
            _chipsRow.AddToClassList("fm-meal-day-picker-row");
            Add(_chipsRow);
        }

        /// <summary>Rebuilds the chips for the last week ending today. Call again when the language changes.</summary>
        public void Build(DateTime nowLocal)
        {
            _chipsRow.Clear();
            _chips.Clear();

            IReadOnlyList<DateTime> days = MealLogDate.AvailableDays(nowLocal);
            CultureInfo culture = LocalizationSettings.SelectedLocale?.Identifier.CultureInfo ?? CultureInfo.CurrentCulture;

            for (int i = days.Count - 1; i >= 0; i--)
            {
                DateTime day = days[i];
                var chip = new FMButton
                {
                    title = Label(day, nowLocal, culture),
                    size = Size.S,
                    variant = ButtonVariant.Default
                };
                chip.AddToClassList("fm-meal-day-picker-chip");
                chip.clicked += () => DaySelected?.Invoke(day);
                _chipsRow.Add(chip);
                _chips.Add((day, chip));
            }

            if (_selectedDay == default)
            {
                _selectedDay = nowLocal.Date;
            }
            UpdateSelection();
        }

        public static string Label(DateTime day, DateTime nowLocal, CultureInfo culture)
        {
            int daysAgo = (nowLocal.Date - day.Date).Days;
            if (daysAgo == 0)
            {
                return LocalizationSettings.StringDatabase.GetLocalizedString("UI", "TODAY");
            }
            if (daysAgo == 1)
            {
                return LocalizationSettings.StringDatabase.GetLocalizedString("UI", "YESTERDAY");
            }
            string label = day.ToString("ddd d", culture);
            return label.Length > 0 ? char.ToUpper(label[0], culture) + label.Substring(1) : label;
        }

        /// <summary>One button node per chip (selected state included); the screen removes them on teardown.</summary>
        public List<AccessibilityNode> AddAccessibilityNodes(AccessibilityHierarchy hierarchy)
        {
            var nodes = new List<AccessibilityNode>();
            if (hierarchy == null)
            {
                return nodes;
            }

            foreach (var (day, chip) in _chips)
            {
                AccessibilityNode node = hierarchy.AddNode(chip.title);
                node.role = AccessibilityRole.Button;
                if (day == _selectedDay)
                {
                    node.state = AccessibilityState.Selected;
                }
                FMButton target = chip;
                node.frameGetter = () =>
                {
                    if (target.panel == null)
                    {
                        return Rect.zero;
                    }
                    var r = target.worldBound;
                    var s = target.panel.scaledPixelsPerPoint;
                    return new Rect(r.position * s, r.size * s);
                };
                DateTime captured = day;
                node.invoked += () =>
                {
                    DaySelected?.Invoke(captured);
                    return true;
                };
                nodes.Add(node);
            }
            return nodes;
        }

        private void UpdateSelection()
        {
            foreach (var (day, chip) in _chips)
            {
                bool selected = day == _selectedDay;
                chip.variant = selected ? ButtonVariant.Accent : ButtonVariant.Default;
                chip.EnableInClassList("fm-meal-day-picker-chip--selected", selected);
            }
        }
    }
}
