using UnityEngine;
using UnityEngine.UIElements;

namespace eu.foodmission.platform
{
    /// <summary>
    /// Draws the caret of focused, empty text fields where the first character will appear.
    /// UI Toolkit places it one unscaled line height below the top of the text (Unity 6.3–6.7,
    /// <c>TextInfo.GetCursorPositionFromStringIndexUsingLineHeight</c> with no characters), ignoring
    /// the text alignment and the font asset scale: with App UI's middle-left fields it sits ~19 px
    /// too high with Roboto and ~4 px too low with OpenDyslexic, and jumps into place on the first key.
    /// While the field is empty the native caret is hidden (<see cref="HiddenNativeCaretClass"/>)
    /// and a bar is drawn inside the text element instead. Attached once to the panel root, so it
    /// covers every field, popups included.
    /// </summary>
    public sealed class EmptyFieldCaret
    {
        public const string HiddenNativeCaretClass = "fm-empty-caret";
        public const string CaretBarClass = "fm-empty-caret__bar";

        // Class App UI's BlinkingCursor toggles to blink the native caret
        private const string AppUIBlinkOffClass = "appui-text-cursor--transparent";
        private const string FieldClass = "unity-base-text-field";
        private const string NativePlaceholderClass = "unity-base-text-field__input--placeholder";
        private const string InnerTextClass = "unity-text-element--inner-input-field-component";
        private const float CaretWidth = 1f;
        private const long RefreshIntervalMs = 50;

        private VisualElement _root;
        private TextElement _text;
        private VisualElement _field;
        private VisualElement _bar;
        private IVisualElementScheduledItem _refresh;

        public static Rect ComputeCaretRect(Rect content, float lineHeight, float width, TextAnchor align)
        {
            float x;
            switch (align)
            {
                case TextAnchor.UpperCenter:
                case TextAnchor.MiddleCenter:
                case TextAnchor.LowerCenter:
                    x = content.x + (content.width - width) * 0.5f;
                    break;
                case TextAnchor.UpperRight:
                case TextAnchor.MiddleRight:
                case TextAnchor.LowerRight:
                    x = content.xMax - width;
                    break;
                default:
                    x = content.x;
                    break;
            }

            float y;
            switch (align)
            {
                case TextAnchor.MiddleLeft:
                case TextAnchor.MiddleCenter:
                case TextAnchor.MiddleRight:
                    y = content.y + (content.height - lineHeight) * 0.5f;
                    break;
                case TextAnchor.LowerLeft:
                case TextAnchor.LowerCenter:
                case TextAnchor.LowerRight:
                    y = content.yMax - lineHeight;
                    break;
                default:
                    y = content.y;
                    break;
            }

            return new Rect(x, y, width, lineHeight);
        }

        public static bool ShouldDraw(string text, bool isReadOnly, bool showsPlaceholder)
        {
            return string.IsNullOrEmpty(text) && !isReadOnly && !showsPlaceholder;
        }

        public void Attach(VisualElement root)
        {
            if (root == null)
            {
                Debug.LogError("[EmptyFieldCaret] Root element is null");
                return;
            }

            if (root.panel != null)
            {
                Register(root.panel.visualTree);
            }
            else
            {
                root.RegisterCallback<AttachToPanelEvent>(OnRootAttachedToPanel);
            }
        }

        private void OnRootAttachedToPanel(AttachToPanelEvent evt)
        {
            ((VisualElement)evt.target).UnregisterCallback<AttachToPanelEvent>(OnRootAttachedToPanel);
            Register(evt.destinationPanel.visualTree);
        }

        private void Register(VisualElement root)
        {
            _root = root;
            _root.RegisterCallback<FocusInEvent>(OnFocusIn, TrickleDown.TrickleDown);
            _root.RegisterCallback<FocusOutEvent>(OnFocusOut, TrickleDown.TrickleDown);
        }

        private void OnFocusIn(FocusInEvent evt)
        {
            var text = FindInnerText(evt.target as VisualElement);
            if (text == _text)
            {
                return;
            }

            Untrack();
            if (text != null)
            {
                Track(text);
            }
        }

        private void OnFocusOut(FocusOutEvent evt)
        {
            if (_text != null && evt.target == _text)
            {
                Untrack();
            }
        }

        private static TextElement FindInnerText(VisualElement target)
        {
            if (target is TextElement text && text.ClassListContains(InnerTextClass))
            {
                return text;
            }

            if (target != null && target.ClassListContains(FieldClass))
            {
                return target.Q<TextElement>(className: InnerTextClass);
            }

            return null;
        }

        private void Track(TextElement text)
        {
            _text = text;
            _field = FindAncestorWithClass(text, FieldClass);
            _bar = new VisualElement { name = "fm-empty-caret-bar", pickingMode = PickingMode.Ignore };
            _bar.AddToClassList(CaretBarClass);
            _text.Add(_bar);
            _refresh = _text.schedule.Execute(Refresh).Every(RefreshIntervalMs);
            Refresh();
        }

        private void Untrack()
        {
            _refresh?.Pause();
            _refresh = null;
            _field?.RemoveFromClassList(HiddenNativeCaretClass);
            _bar?.RemoveFromHierarchy();
            _bar = null;
            _field = null;
            _text = null;
        }

        private void Refresh()
        {
            if (_text == null || _text.panel == null)
            {
                Untrack();
                return;
            }

            var showsPlaceholder = FindAncestorWithClass(_text, NativePlaceholderClass) != null;
            var draw = ShouldDraw(_text.text, ((ITextEdition)_text).isReadOnly, showsPlaceholder);
            _field?.EnableInClassList(HiddenNativeCaretClass, draw);
            if (!draw)
            {
                _bar.style.display = DisplayStyle.None;
                return;
            }

            var lineHeight = _text.MeasureTextSize("​", 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined).y;
            // Absolute children are offset from the padding box, so the content box starts at the padding
            var resolved = _text.resolvedStyle;
            var content = new Rect(resolved.paddingLeft, resolved.paddingTop, _text.contentRect.width, _text.contentRect.height);
            var rect = ComputeCaretRect(content, lineHeight, CaretWidth, resolved.unityTextAlign);

            _bar.style.display = DisplayStyle.Flex;
            _bar.style.left = rect.x;
            _bar.style.top = rect.y;
            _bar.style.width = rect.width;
            _bar.style.height = rect.height;
            // Follow App UI's blink of the (hidden) native caret
            var blinkOff = _field != null && _field.ClassListContains(AppUIBlinkOffClass);
            _bar.style.visibility = blinkOff ? Visibility.Hidden : Visibility.Visible;
        }

        private static VisualElement FindAncestorWithClass(VisualElement element, string className)
        {
            for (var current = element; current != null; current = current.parent)
            {
                if (current.ClassListContains(className))
                {
                    return current;
                }
            }

            return null;
        }
    }
}
