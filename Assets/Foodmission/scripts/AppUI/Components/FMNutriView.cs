using System;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Properties;
using Unity.AppUI.MVVM;

namespace eu.foodmission.platform.Components
{
    [UxmlElement]
    public partial class FMNutriView : VisualElement
    {
        private Image _nutriImage;
        private INutriService _nutriService;
        private IAudioService _audioService;

        // Static count to manage overlapping/simultaneous views (e.g., during screen transitions)
        private static int s_ActiveViewsCount = 0;

        /// <summary>
        /// Callback executed when Nutri is clicked.
        /// </summary>
        public Action OnClick { get; set; }

        /// <summary>
        /// Indicates whether a click action callback has been assigned.
        /// </summary>
        public bool HasClickAction => OnClick != null;

        public FMNutriView()
        {
            AddToClassList("fm-nutri-view");

            // Internal image that renders the mascot texture
            _nutriImage = new Image();
            _nutriImage.AddToClassList("fm-nutri-view__image");
            _nutriImage.scaleMode = ScaleMode.ScaleAndCrop;
            Add(_nutriImage);

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            RegisterCallback<ClickEvent>(OnNutriClicked);
        }

        private void OnNutriClicked(ClickEvent evt)
        {
            if (OnClick == null)
            {
                // Default action: play Greeting animation, then return to Idle after 1 second
                _nutriService?.SetAction(NutriAction.Greeting);
                _audioService?.PlayNutriSfx(NutriSfxType.Touch);

                schedule.Execute(() =>
                {
                    _nutriService?.SetAction(NutriAction.Idle);
                }).ExecuteLater(1500);
            }
            else
            {
                OnClick.Invoke();
            }
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            _nutriService = App.current?.services?.GetService<INutriService>();
            _audioService = App.current?.services?.GetService<IAudioService>();
            if (_nutriService != null)
            {
                if (s_ActiveViewsCount == 0)
                {
                    _nutriService.SetActive(true);
                    _nutriService.SetCameraActive(true);
                }

                s_ActiveViewsCount++;

                // Bind the render texture to our image
                _nutriImage.image = _nutriService.NutriCameraRenderTexture;
            }
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            _nutriImage.image = null;

            if (_nutriService != null)
            {
                s_ActiveViewsCount = Mathf.Max(0, s_ActiveViewsCount - 1);

                if (s_ActiveViewsCount == 0)
                {
                    _nutriService.SetCameraActive(false);
                    _nutriService.SetActive(false);
                }
                _nutriService = null;
            }
        }
    }
}
