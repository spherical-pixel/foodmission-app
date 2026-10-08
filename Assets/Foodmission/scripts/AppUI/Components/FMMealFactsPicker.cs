using System;
using System.Collections.Generic;

using MainraGames;

using Unity.AppUI.Core;
using Unity.AppUI.MVVM;
using Unity.AppUI.UI;

using UnityEngine.UIElements;

namespace eu.foodmission.platform.Components
{
    /// <summary>
    /// Collapsible sections of meal flags and swaps (<see cref="MealFacts"/>). Renders only;
    /// the owner handles the events and calls <see cref="SetContent"/> again when its data changes.
    /// </summary>
    public class FMMealFactsPicker : VisualElement
    {
        private readonly IAudioService _audioService;

        public event Action<string> SectionToggled;
        public event Action<string> ItemToggled;
        public event Action<string, string> SwapSelected;

        public FMMealFactsPicker()
        {
            AddToClassList("fm-quick-meal-questions-list");
            _audioService = App.current?.services?.GetService<IAudioService>();
        }

        public void SetContent(IReadOnlyList<QuickMealSection> sections, IReadOnlyList<QuickMealCheckItem> fallbackItems = null)
        {
            Clear();

            if (sections != null && sections.Count > 0)
            {
                foreach (QuickMealSection section in sections)
                {
                    if (section != null)
                    {
                        Add(BuildSection(section));
                    }
                }
            }
            else if (fallbackItems != null)
            {
                foreach (QuickMealCheckItem item in fallbackItems)
                {
                    Add(BuildCard(item));
                }
            }
        }

        private VisualElement BuildSection(QuickMealSection sec)
        {
            var sectionBox = new VisualElement();
            sectionBox.AddToClassList("fm-quick-meal-accordion-section");

            var accordionHeader = new VisualElement();
            accordionHeader.AddToClassList("fm-quick-meal-accordion-header");
            if (sec.IsExpanded)
            {
                accordionHeader.AddToClassList("fm-quick-meal-accordion-header--expanded");
            }

            var headerLeft = new VisualElement();
            headerLeft.AddToClassList("fm-quick-meal-accordion-header-left");

            if (!string.IsNullOrEmpty(sec.Icon))
            {
                var iconText = new Unity.AppUI.UI.Text();
                iconText.text = sec.Icon;
                iconText.AddToClassList("fm-quick-meal-accordion-icon");
                headerLeft.Add(iconText);
            }

            var titleText = new Unity.AppUI.UI.Text();
            titleText.AddToClassList("fm-quick-meal-accordion-title");
            titleText.text = sec.Title ?? "";
            titleText.size = TextSize.M;
            headerLeft.Add(titleText);
            accordionHeader.Add(headerLeft);

            var headerRight = new VisualElement();
            headerRight.AddToClassList("fm-quick-meal-accordion-header-right");

            int selectedCount = sec.SelectedCount;
            if (selectedCount > 0)
            {
                var badge = new Unity.AppUI.UI.Text();
                badge.AddToClassList("fm-quick-meal-accordion-badge");
                badge.text = $"{selectedCount}";
                headerRight.Add(badge);
            }

            var chevron = new Icon();
            chevron.AddToClassList("fm-quick-meal-accordion-chevron");
            chevron.iconName = "caret-down";
            if (!sec.IsExpanded)
            {
                chevron.AddToClassList("fm-quick-meal-accordion-chevron--collapsed");
            }
            headerRight.Add(chevron);
            accordionHeader.Add(headerRight);

            string capturedSecId = sec.Id;
            accordionHeader.RegisterCallback<ClickEvent>(_ =>
            {
                PlayClick();
                SectionToggled?.Invoke(capturedSecId);
            });

            sectionBox.Add(accordionHeader);

            var contentBox = new VisualElement();
            contentBox.AddToClassList("fm-quick-meal-accordion-content");
            if (!sec.IsExpanded)
            {
                contentBox.AddToClassList("fm-quick-meal-accordion-content--hidden");
            }

            if (sec.Items != null)
            {
                foreach (var q in sec.Items)
                {
                    contentBox.Add(BuildCard(q));
                }
            }

            sectionBox.Add(contentBox);

            return sectionBox;
        }

        private VisualElement BuildCard(QuickMealCheckItem q)
        {
            if (q == null)
            {
                return new VisualElement();
            }

            var card = new VisualElement();
            card.AddToClassList("fm-quick-meal-question-card");
            if (q.IsChecked)
            {
                card.AddToClassList("fm-quick-meal-question-card--checked");
            }

            bool isSwapSelector = q.QuestionType == DirectQuestionType.SwapSelector;
            string capturedId = q.Id;

            var header = new VisualElement();
            header.AddToClassList("fm-quick-meal-question-header");

            if (!isSwapSelector)
            {
                var checkbox = new Unity.AppUI.UI.Checkbox
                {
                    value = q.IsChecked ? CheckboxState.Checked : CheckboxState.Unchecked
                };
                checkbox.AddToClassList("fm-quick-meal-checkbox");
                checkbox.RegisterValueChangedCallback(_ =>
                {
                    PlayClick();
                    ItemToggled?.Invoke(capturedId);
                });
                header.Add(checkbox);

                header.RegisterCallback<ClickEvent>(evt =>
                {
                    if (evt.target is Unity.AppUI.UI.Checkbox ||
                        (evt.target is VisualElement ve && ve.GetFirstAncestorOfType<Unity.AppUI.UI.Checkbox>() != null))
                    {
                        return;
                    }

                    PlayClick();
                    ItemToggled?.Invoke(capturedId);
                });
            }

            if (!string.IsNullOrEmpty(q.Icon))
            {
                var iconLabel = new Unity.AppUI.UI.Text();
                iconLabel.text = q.Icon;
                iconLabel.AddToClassList("fm-quick-meal-question-icon");
                header.Add(iconLabel);
            }

            var promptLabel = new Unity.AppUI.UI.Text();
            promptLabel.text = q.Prompt ?? "";
            promptLabel.AddToClassList("fm-quick-meal-question-text");
            header.Add(promptLabel);

            card.Add(header);

            // Render inline swap options directly if question is a SwapSelector
            if (isSwapSelector && q.SwapOptions != null && q.SwapOptions.Length > 0)
            {
                var swapsContainer = new VisualElement();
                swapsContainer.AddToClassList("fm-quick-meal-swaps-container");

                var swapsTitle = new Unity.AppUI.UI.Text();
                swapsTitle.text = "@UI:QUICK_MEAL_SWAPS_SUBTITLE";
                swapsTitle.AddToClassList("fm-quick-meal-swaps-title");
                swapsContainer.Add(swapsTitle);

                var swapsList = new VisualElement();
                swapsList.AddToClassList("fm-quick-meal-swaps-list");

                foreach (var swap in q.SwapOptions)
                {
                    if (string.IsNullOrEmpty(swap))
                    {
                        continue;
                    }

                    var chip = new VisualElement();
                    chip.AddToClassList("fm-quick-meal-swap-chip");
                    bool isSelected = q.IsChecked && swap == q.SelectedSwapOption;
                    if (isSelected)
                    {
                        chip.AddToClassList("fm-quick-meal-swap-chip--active");
                    }

                    var radio = new Unity.AppUI.UI.Radio
                    {
                        value = isSelected,
                        size = Size.M
                    };
                    radio.AddToClassList("fm-quick-meal-swap-radio");
                    chip.Add(radio);

                    var chipLabel = new Unity.AppUI.UI.Text();
                    chipLabel.AddToClassList("fm-quick-meal-swap-chip-text");
                    chipLabel.text = SwapLocalization.GetSwapDisplayName(swap);
                    chip.Add(chipLabel);

                    string capturedSwap = swap;
                    chip.RegisterCallback<ClickEvent>(evt =>
                    {
                        evt.StopPropagation();
                        PlayClick();
                        SwapSelected?.Invoke(capturedId, capturedSwap);
                    });

                    swapsList.Add(chip);
                }

                swapsContainer.Add(swapsList);
                card.Add(swapsContainer);
            }

            return card;
        }

        private void PlayClick()
        {
            _audioService?.PlaySfx(SfxType.PositiveButton);
        }
    }
}
