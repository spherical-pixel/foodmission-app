using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

using Unity.AppUI.Core;
using Unity.AppUI.MVVM;
using Unity.AppUI.UI;

using UnityEngine.UIElements;

namespace eu.foodmission.platform.Components
{
    /// <summary>
    /// Full-screen picker with a search bar on top of a filtered list, for dropdowns with
    /// long option lists (country, region). The App UI Dropdown menu cannot be filtered and
    /// is unusable on a phone with dozens of options.
    /// </summary>
    public static class FMSearchablePicker
    {
        public static void Show(
            VisualElement anchor,
            string title,
            IList<string> options,
            int selectedIndex,
            Action<int> onSelected)
        {
            if (anchor == null || options == null || options.Count == 0)
            {
                return;
            }

            Modal modal = null;
            var filtered = Filter(options, "");

            var root = new VisualElement();
            root.AddToClassList("fm-info-dialog");
            root.AddToClassList("fm-searchable-picker");

            var heading = new Heading(title) { size = HeadingSize.M };
            heading.AddToClassList("fm-info-dialog__heading");
            root.Add(heading);

            var searchBar = new SearchBar { placeholder = "@UI:SEARCH_PLACEHOLDER", size = Size.L };
            searchBar.AddToClassList("fm-searchable-picker__search");
            root.Add(searchBar);

            var emptyText = new Unity.AppUI.UI.Text("@UI:NO_RESULTS") { size = TextSize.M };
            emptyText.AddToClassList("fm-searchable-picker__empty");
            emptyText.AddToClassList("fm-searchable-picker__empty--hidden");
            root.Add(emptyText);

            var listView = new ListView
            {
                itemsSource = filtered,
                selectionType = SelectionType.None,
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight
            };
            listView.AddToClassList("fm-searchable-picker__list");
            listView.makeItem = () =>
            {
                var item = new Unity.AppUI.UI.Text { size = TextSize.L };
                item.AddToClassList("fm-searchable-picker__item");
                item.AddManipulator(new Pressable(() =>
                {
                    if (item.userData is int index)
                    {
                        onSelected?.Invoke(index);
                        modal?.Dismiss(DismissType.Action);
                    }
                }));
                return item;
            };
            listView.bindItem = (element, position) =>
            {
                var optionIndex = filtered[position];
                var item = (Unity.AppUI.UI.Text)element;
                item.userData = optionIndex;
                item.text = options[optionIndex];
                item.EnableInClassList("fm-searchable-picker__item--selected", optionIndex == selectedIndex);
            };
            root.Add(listView);

            searchBar.RegisterValueChangingCallback(evt =>
            {
                filtered.Clear();
                filtered.AddRange(Filter(options, evt.newValue));
                listView.RefreshItems();
                listView.ScrollToItem(0);
                emptyText.EnableInClassList("fm-searchable-picker__empty--hidden", filtered.Count > 0);
            });

            var cancelButton = new Unity.AppUI.UI.Button
            {
                title = "@UI:TXT_CANCEL",
                variant = ButtonVariant.Default
            };
            cancelButton.AddToClassList("fm-button");
            cancelButton.clicked += () => modal?.Dismiss(DismissType.Cancel);
            root.Add(cancelButton);

            // Full-screen takeover: keep the content out of the notch and the home indicator
            var safeArea = new VisualElement();
            safeArea.AddToClassList("fm-searchable-picker__safe-area");
            App.current?.services?.GetService<IThemeService>()?.ApplySafeAreaPadding(safeArea, true, true, true, true);
            safeArea.Add(root);

            modal = Modal.Build(anchor, safeArea);
            modal.SetFullScreenMode(ModalFullScreenMode.FullScreenTakeOver);
            // Focus once the modal is on screen, so the user can type right away
            modal.shown += _ => searchBar.schedule.Execute(() => searchBar.Focus());
            modal.Show();

            if (selectedIndex >= 0)
            {
                var position = filtered.IndexOf(selectedIndex);
                if (position >= 0)
                {
                    listView.schedule.Execute(() => listView.ScrollToItem(position)).ExecuteLater(50);
                }
            }
        }

        /// <summary>
        /// Indices of the options matching <paramref name="query"/>, ignoring case and accents.
        /// Options where a word starts with the query come first; both groups keep the original order.
        /// An empty query returns every index.
        /// </summary>
        public static List<int> Filter(IList<string> options, string query)
        {
            var result = new List<int>();
            if (options == null)
            {
                return result;
            }

            var normalizedQuery = Normalize(query).Trim();
            if (normalizedQuery.Length == 0)
            {
                for (int i = 0; i < options.Count; i++)
                {
                    result.Add(i);
                }
                return result;
            }

            var containsOnly = new List<int>();
            for (int i = 0; i < options.Count; i++)
            {
                var option = Normalize(options[i]);
                var position = option.IndexOf(normalizedQuery, StringComparison.Ordinal);
                if (position < 0)
                {
                    continue;
                }

                if (StartsAWord(option, normalizedQuery))
                {
                    result.Add(i);
                }
                else
                {
                    containsOnly.Add(i);
                }
            }

            result.AddRange(containsOnly);
            return result;
        }

        /// <summary>Lower case without diacritics ("Österreich" → "osterreich", "Łódzkie" → "lodzkie").</summary>
        public static string Normalize(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "";
            }

            var decomposed = value.ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(decomposed.Length);
            foreach (var c in decomposed)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                // Letters that do not decompose into base letter + mark
                switch (c)
                {
                    case 'ø': builder.Append('o'); break;
                    case 'æ': builder.Append("ae"); break;
                    case 'œ': builder.Append("oe"); break;
                    case 'ł': builder.Append('l'); break;
                    case 'đ': builder.Append('d'); break;
                    case 'ß': builder.Append("ss"); break;
                    default: builder.Append(c); break;
                }
            }
            return builder.ToString();
        }

        private static bool StartsAWord(string option, string query)
        {
            var position = option.IndexOf(query, StringComparison.Ordinal);
            while (position >= 0)
            {
                if (position == 0 || !char.IsLetterOrDigit(option[position - 1]))
                {
                    return true;
                }
                position = option.IndexOf(query, position + 1, StringComparison.Ordinal);
            }
            return false;
        }
    }
}
