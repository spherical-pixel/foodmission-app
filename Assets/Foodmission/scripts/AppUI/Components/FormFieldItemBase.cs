
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.UIElements;
using Unity.Properties;
using System;


namespace eu.foodmission.platform.Components
{
    [UxmlElement]
    public partial class FormFieldItemBase : VisualElement, IAccessibleComponent
    {

        /* ========= UXML ATTRIBUTES ========= */

        [UxmlAttribute("Heading-Text")]
        [CreateProperty]

        public string HeadingText
        {
            get => _heading?.text ?? "";
            set
            {
                if (_heading != null)
                {
                    _heading.text = value;
                }
            }
        }

        [UxmlAttribute("iconButton-Visible")]
        [CreateProperty]
        public bool IconButtonVisible
        {
            get => _iconButton?.visible ?? false;
            set
            {
                if (_iconButton != null)
                {
                    _iconButton.visible = value;
                }
            }
        }

        [UxmlAttribute("iconButton-icon")]
        [CreateProperty]
        public string IconButtonIcon
        {
            get => _iconButton?.icon ?? "";
            set
            {
                if (_iconButton != null)
                {
                    _iconButton.icon = value;
                }
            }
        }

        [UxmlAttribute("iconButton-quiet")]
        [CreateProperty]
        public bool IconButtonQuiet
        {
            get => _iconButton?.quiet ?? false;
            set
            {
                if (_iconButton != null)
                {
                    _iconButton.quiet = value;
                }
            }
        }


        [UxmlAttribute("helpText-Text")]
        [CreateProperty]
        public string HelpTextText
        {
            get => _helpText?.text ?? "";
            set
            {
                if (_helpText != null)
                {
                    _helpText.text = value;

                    if (value != string.Empty)
                    {
                        _helpText.style.visibility = Visibility.Visible;
                        _helpSpacer.style.visibility = Visibility.Visible;
                    }
                    else
                    {
                        _helpText.style.visibility = Visibility.Hidden;
                        _helpSpacer.style.visibility = Visibility.Hidden;
                    }
                }
            }
        }

        [UxmlAttribute("helpText-Variant")]
        [CreateProperty]
        public HelpTextVariant HelpTextVariant
        {
            get => _helpText?.variant ?? HelpTextVariant.Default;
            set
            {
                if (_helpText != null)
                {
                    _helpText.variant = value;
                }
            }
        }


        /* ========= INTERNAL ELEMENTS ========= */
        protected VisualElement _headingContainer;
        protected Unity.AppUI.UI.Heading _heading;
        protected Unity.AppUI.UI.IconButton _iconButton;
        protected VisualElement _fieldContainer;
        protected Unity.AppUI.UI.HelpText _helpText;
        protected Spacer _helpSpacer;

        protected AccessibilityNode _accessibilityNode;

        public AccessibilityNode AccessibilityNode => _accessibilityNode;

        public virtual AccessibilityNode CreateAccessibilityNode(AccessibilityHierarchy hierarchy, string label)
        {
            DestroyAccessibilityNode();
            _accessibilityNode = hierarchy.AddNode(!string.IsNullOrEmpty(label) ? label : HeadingText);
            return _accessibilityNode;
        }

        public virtual void UpdateAccessibilityNode() { }

        public virtual void DestroyAccessibilityNode()
        {
            _accessibilityNode = null;
        }

        protected static Func<Rect> MakeFrameGetter(VisualElement element)
        {
            return () =>
            {
                if (element == null || element.panel == null) return Rect.zero;
                var rect = element.worldBound;
                var scale = element.panel.scaledPixelsPerPoint;
                return new Rect(rect.position * scale, rect.size * scale);
            };
        }

        public FormFieldItemBase()
        {
            _headingContainer = new VisualElement();
            _headingContainer.style.flexDirection = FlexDirection.Row;
            _headingContainer.style.justifyContent = Justify.SpaceBetween;
            this.Add(_headingContainer);

            _heading = new Unity.AppUI.UI.Heading();
            //_heading.AddToClassList("heading-wrap");
            _heading.style.whiteSpace = WhiteSpace.Normal;
            _heading.AddToClassList("heading_field");
            _heading.size = HeadingSize.S;
            _heading.primary = true;

            _headingContainer.Add(_heading);

            _iconButton = new Unity.AppUI.UI.IconButton();
            _headingContainer.Add(_iconButton);

            _fieldContainer = new VisualElement();
            this.Add(_fieldContainer);

            _helpSpacer = new Spacer();
            _helpSpacer.spacing = SpacerSpacing.S;
            this.Add(_helpSpacer);
            _helpSpacer.style.visibility = Visibility.Hidden;

            _helpText = new Unity.AppUI.UI.HelpText();
            _helpText.style.visibility = Visibility.Hidden;
            this.Add(_helpText);

            IconButtonVisible = false;

        }



    }
}