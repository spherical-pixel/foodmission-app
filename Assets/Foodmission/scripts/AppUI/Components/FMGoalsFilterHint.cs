using System;

using Unity.AppUI.UI;

using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;

namespace eu.foodmission.platform.Components
{
    /// <summary>
    /// Footer for content lists filtered by the user's goals (<see cref="GoalContentFilter"/>):
    /// tells the user some topics are hidden and offers a shortcut to edit their goals.
    /// Hidden until <see cref="SetVisible"/> is called with true.
    /// </summary>
    public class FMGoalsFilterHint : VisualElement
    {
        private const string HiddenClass = "fm-goals-hint--hidden";

        private readonly Text _text;
        private readonly Unity.AppUI.UI.Button _button;

        public event Action clicked;

        public FMGoalsFilterHint()
        {
            AddToClassList("fm-goals-hint");
            AddToClassList(HiddenClass);

            _text = new Text();
            _text.AddToClassList("fm-goals-hint__text");
            Add(_text);

            _button = new Unity.AppUI.UI.Button();
            _button.AddToClassList("fm-goals-hint__button");
            _button.clicked += () => clicked?.Invoke();
            Add(_button);

            RefreshTexts();
        }

        public void RefreshTexts()
        {
            _text.text = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "GOALS_FILTER_HINT");
            _button.title = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "GOALS_FILTER_EDIT");
        }

        public void SetVisible(bool visible)
        {
            EnableInClassList(HiddenClass, !visible);
        }
    }
}
