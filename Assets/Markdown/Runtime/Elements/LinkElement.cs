using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace BrewedInk.MarkdownSupport
{
    public class LinkElement : TextElement
    {
        private VisualElement _line;
        private Label _label;

        private string _preText;
        private string _title;
        private Label _fakeLabel;
        private List<int> _indexes;
        private string _firstWord;
        private UMarkdownContext _context;
        private MarkdownLinkUri _uri;

        public LinkElement(UMarkdownContext context, VisualElement line, Label label, string title, MarkdownLinkUri uri, string tooltip)
        {
            _uri = uri;
            _context = context;
            _title = title;
            _label = label;
            _line = line;

            _firstWord = title.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
            _preText = _label.text + title;
            this.tooltip = tooltip;
           
            _indexes = new List<int>();
            for (var i = 0; i < _preText.Length; i++)
            {
                var isSplit = _preText[i] == ' ' || i == _preText.Length - 1;
                if (isSplit)
                {
                    _indexes.Add(i);
                }
            }
            _title = title;
            _fakeLabel = new Label($"<alpha=#00>{_label.text}<alpha=#FF><u>{title}</u>");
            _fakeLabel.AddToClassList("link-hidden");
            line.Insert(0, _fakeLabel);

            label.text += $"<alpha=#00>{title}<alpha=#FF>";
            this.text = $"{title}";


            
            Action();
            
            AddToClassList("link");
            
            RegisterCallback<MouseDownEvent>(OnClicked);
            RegisterCallback<MouseEnterEvent>(OnMouseEnter);
            RegisterCallback<MouseLeaveEvent>(OnMouseLeave);
            line.RegisterCallback<GeometryChangedEvent>(OnLineChanged);

        }

        private void OnMouseLeave(MouseLeaveEvent evt)
        {
            _fakeLabel.RemoveFromClassList("hover");
        }

        private void OnMouseEnter(MouseEnterEvent evt)
        {
            _fakeLabel.AddToClassList("hover");
        }

        bool Try()
        {
            var measure = _fakeLabel.MeasureTextSize(_fakeLabel.text, 0, MeasureMode.Undefined, 0, MeasureMode.Undefined);
            if (float.IsNaN(measure.x)) return false;

            var xMax = _fakeLabel.localBound.width;
            var measure2 = _fakeLabel.MeasureTextSize(_fakeLabel.text, xMax, MeasureMode.AtMost, 0, MeasureMode.Undefined);
            var linkMeasure = _fakeLabel.MeasureTextSize(_title, xMax, MeasureMode.Undefined, 0, MeasureMode.Undefined);
            
            var yPos = measure2.y;

            if (measure2.x >= xMax)
            {
                for (var i = _indexes.Count - 1; i >= 0; i--)
                {
                    // we need to remove the previous lines of text, so as to 
                    // get the last line, and be able to measure its x-size.

                    // we can start slicing down words until the Y height lowers,
                    // indicating that a line has been recovered. 
                    // then, the set of words we removed IS the text on the final line.
                    var nextText = _preText.Substring(0, _indexes[i]);
                    measure2 = _fakeLabel.MeasureTextSize(nextText, xMax, MeasureMode.AtMost, 0, MeasureMode.Undefined);
                    if (measure2.y < yPos)
                    {
                        var removedText = _preText.Substring(_indexes[i]);
                        measure2 = _fakeLabel.MeasureTextSize(removedText, xMax, MeasureMode.AtMost, 0, MeasureMode.Undefined);
                        
                        break;
                    }
                }

            }


            style.left = measure2.x - linkMeasure.x;
            style.top = yPos - localBound.height;
            return true;

        }

        void Action()
        {
            if (!Try())
            {
                schedule.Execute(Action);
            }
        }

        MarkdownVisualElement FindRoot()
        {
            var candidate = parent;
            while (candidate != null)
            {
                if (candidate is MarkdownVisualElement root)
                    return root;
                candidate = candidate.parent;
            }
            return null;
        }

        private void OnLineChanged(GeometryChangedEvent evt)
        {
            Action();
        }

        private void OnClicked(MouseDownEvent evt)
        {
            _uri.Open(FindRoot());
        }
    }
}