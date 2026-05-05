
using Unity.AppUI.UI;
using UnityEngine.UIElements;
using Unity.Properties;


namespace eu.foodmission.platform.Components
{
    [UxmlElement]
    public partial class FMStatusBar : VisualElement
    {

        /* ========= UXML ATTRIBUTES ========= */

        [UxmlAttribute("Heading-Emoji")]
        [CreateProperty]

		public string HeadingEmoji
        {
            get => _headingIcon?.text ?? "";
            set
            {
                if( _headingIcon != null)
                {
                    _headingIcon.text = value;
                }
            }
        }

        [UxmlAttribute("Heading-Text")]
        [CreateProperty]

		public string HeadingText
        {
            get => _headingText?.text ?? "";
            set
            {
                if( _headingText != null)
                {
                    _headingText.text = value;
                }
            }
        }

        [UxmlAttribute("Progress-Value")]
        [CreateProperty]

		public float ProgressValue
        {
            get => _linearProgress?.value ?? 0f;
            set
            {
                if( _linearProgress != null)
                {
                    _linearProgress.value = value;
                }
            }
        }

        
        /* ========= INTERNAL ELEMENTS ========= */
        protected Unity.AppUI.UI.Heading _headingIcon;
        protected Unity.AppUI.UI.Heading _headingText;
        protected Unity.AppUI.UI.LinearProgress _linearProgress;


        public FMStatusBar()
        {
            AddToClassList("fm-status-row");

            _headingIcon = new Unity.AppUI.UI.Heading();
            _headingIcon.AddToClassList("centered-text");
            _headingIcon.size = HeadingSize.L;
            _headingIcon.primary = true;
            _headingIcon.style.paddingTop = 0;
            _headingIcon.style.paddingBottom = 0;
            Add(_headingIcon);

            _headingText = new Unity.AppUI.UI.Heading();
            _headingText.AddToClassList("centered-text");
            _headingText.AddToClassList("heading-auto-size-md");
            _headingText.size = HeadingSize.M;
            _headingText.primary = true;
            _headingText.style.paddingTop = 0;
            _headingText.style.paddingBottom = 16;
            Add(_headingText);

            _linearProgress = new Unity.AppUI.UI.LinearProgress();
            _linearProgress.variant = Progress.Variant.Determinate;
            _linearProgress.size  = Size.S;
            _linearProgress.AddToClassList("fm-status-progress");
            
            Add(_linearProgress);
        }

        
        
    }
}