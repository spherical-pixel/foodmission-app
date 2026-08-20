using System;
using System.Collections.Generic;

using Unity.AppUI.UI;
using Unity.Properties;

using UnityEngine;
using UnityEngine.UIElements;

namespace eu.foodmission.platform.Components
{
    public enum QuizResponseState
    {
        Default,
        Selected,
        Correct,
        Incorrect
    }

    [UxmlElement]
    public partial class FMResponseQuiz : ExVisualElement
    {
        private const string ClassSelected = "fm-quiz-response-card--selected";
        private const string ClassCorrect = "fm-quiz-response-card--correct";
        private const string ClassIncorrect = "fm-quiz-response-card--incorrect";

        /* ========= UXML ATTRIBUTES ========= */
        [UxmlAttribute("response-text")]
        [CreateProperty]
        public string ResponseText
        {
            get => _responseText?.text ?? "";
            set
            {
                if (_responseText != null)
                {
                    _responseText.text = value;
                }
            }
        }

        /// <summary>
        /// Controls whether click events are processed. When disabled, clicks are ignored without modifying visual styles.
        /// </summary>
        [UxmlAttribute("clickable")]
        [CreateProperty]
        public bool Clickable { get; set; } = true;

        /// <summary>
        /// Alias for Clickable property.
        /// </summary>
        public bool IsClickable
        {
            get => Clickable;
            set => Clickable = value;
        }

        public QuizOption QuizOption
        {
            get => _quizOption;
            set
            {
                _quizOption = value;
                ResponseText = _quizOption != null ? _quizOption.text : "";
            }
        }

        /* ========= EVENTS & CALLBACKS ========= */
        /// <summary>
        /// Event triggered when this response card is clicked (if Clickable is true).
        /// </summary>
        public event Action<FMResponseQuiz> Clicked;

        /// <summary>
        /// Action callback executed when this response card is clicked (if Clickable is true).
        /// </summary>
        public Action<FMResponseQuiz> OnClick { get; set; }

        /// <summary>
        /// Action callback executed with the associated QuizOption when clicked (if Clickable is true).
        /// </summary>
        public Action<FMResponseQuiz> OnOptionSelected { get; set; }

        /// <summary>
        /// Indicates whether any click callback or event listener is attached.
        /// </summary>
        public bool HasClickAction => OnClick != null || Clicked != null || OnOptionSelected != null;

        /* ========= INTERNAL ELEMENTS ========= */
        protected Unity.AppUI.UI.Text _responseText;
        protected Unity.AppUI.UI.Icon _icon;
        protected QuizOption _quizOption;

        public Unity.AppUI.UI.Text ResponseTextElement => _responseText;
        public Unity.AppUI.UI.Icon IconElement => _icon;

        public FMResponseQuiz()
        {
            this.AddToClassList("fm-quiz-response-card");
            this.AddToClassList("box-background");
            this.AddToClassList("fm-shadow-wrapper");

            _responseText = new Unity.AppUI.UI.Text();
            _responseText.primary = true;
            _responseText.size = TextSize.XL;

            this.Add(_responseText);

            _icon = new Icon();
            _icon.iconName = "fm-arrow-right";

            this.Add(_icon);

            RegisterCallback<ClickEvent>(OnClickEvent);
        }

        private void OnClickEvent(ClickEvent evt)
        {
            if (!Clickable)
            {
                return;
            }

            Clicked?.Invoke(this);
            OnClick?.Invoke(this);
            if (_quizOption != null)
            {
                OnOptionSelected?.Invoke(this);
            }
        }

        public void SetState(QuizResponseState state)
        {
            RemoveFromClassList(ClassSelected);
            RemoveFromClassList(ClassCorrect);
            RemoveFromClassList(ClassIncorrect);
            switch (state)
            {
                case QuizResponseState.Selected:
                    AddToClassList(ClassSelected);
                    _icon.style.display = DisplayStyle.None;
                    break;
                case QuizResponseState.Correct:
                    AddToClassList(ClassCorrect);
                    _icon.style.display = DisplayStyle.None;
                    break;
                case QuizResponseState.Incorrect:
                    AddToClassList(ClassIncorrect);
                    _icon.style.display = DisplayStyle.None;
                    break;
                case QuizResponseState.Default:
                default:
                    // _icon.iconName = "fm-arrow-right";
                    _icon.style.display = DisplayStyle.Flex;
                    break;
            }
        }

        public void ClearContent()
        {
            _responseText.text = "";
            _quizOption = null;
            SetState(QuizResponseState.Default);
        }
    }
}
