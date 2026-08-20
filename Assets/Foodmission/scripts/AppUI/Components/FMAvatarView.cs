using System;
using Unity.AppUI.MVVM;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace eu.foodmission.platform.Components
{
    public enum AvatarViewMode
    {
        Bust,
        FullBody,
        Face2D
    }

    [UxmlElement]
    public partial class FMAvatarView : VisualElement
    {
        private Image _avatarImage;
        private IAvatarService _avatarService;
        private AvatarViewMode _mode = AvatarViewMode.Bust;
        private bool _isAttached = false;

        private static int s_ActiveBustViewsCount = 0;
        private static int s_ActiveFullBodyViewsCount = 0;

        /// <summary>
        /// Display mode of the avatar: Bust (3D head/torso), FullBody (3D full character), or Face2D (2D snapshot).
        /// </summary>
        [UxmlAttribute("mode")]
        public AvatarViewMode Mode
        {
            get => _mode;
            set
            {
                if (_mode != value)
                {
                    if (_isAttached)
                    {
                        DetachMode();
                        _mode = value;
                        AttachMode();
                    }
                    else
                    {
                        _mode = value;
                    }
                }
            }
        }

        /// <summary>
        /// Callback executed when the avatar view is clicked.
        /// </summary>
        public Action OnClick { get; set; }

        /// <summary>
        /// Indicates whether a click action callback has been assigned.
        /// </summary>
        public bool HasClickAction => OnClick != null;

        public FMAvatarView()
        {
            AddToClassList("fm-avatar-view");

            _avatarImage = new Image();
            _avatarImage.AddToClassList("fm-avatar-view__image");
            _avatarImage.scaleMode = ScaleMode.ScaleToFit;
            _avatarImage.style.width = Length.Percent(100);
            _avatarImage.style.height = Length.Percent(100);
            Add(_avatarImage);

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            RegisterCallback<ClickEvent>(OnAvatarClicked);
        }

        private void OnAvatarClicked(ClickEvent evt)
        {
            OnClick?.Invoke();
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            _isAttached = true;
            _avatarService = App.current?.services?.GetService<IAvatarService>();
            AttachMode();
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            _isAttached = false;
            DetachMode();
            _avatarService = null;
        }

        private async void AttachMode()
        {
            if (_avatarService == null) return;

            if (!_avatarService.IsInitialized)
            {
                await _avatarService.InitializeAsync();
                if (!_isAttached) return;
            }

            // Ensure avatar configuration is loaded (user-customized or standard default)
            _avatarService.LoadSavedConfig();

            switch (_mode)
            {
                case AvatarViewMode.FullBody:
                    if (s_ActiveFullBodyViewsCount == 0)
                    {
                        _avatarService.SetFullBodyCameraActive(true);
                    }
                    s_ActiveFullBodyViewsCount++;
                    _avatarImage.image = _avatarService.FullBodyAvatarRenderTexture;
                    break;

                case AvatarViewMode.Bust:
                    if (s_ActiveBustViewsCount == 0)
                    {
                        _avatarService.SetAvatarCameraActive(true);
                    }
                    s_ActiveBustViewsCount++;
                    _avatarImage.image = _avatarService.AvatarCameraRenderTexture;
                    break;

                case AvatarViewMode.Face2D:
                    _avatarService.OnFaceTextureChanged += HandleFaceTextureChanged;
                    _avatarImage.image = _avatarService.GetFaceTexture(allowFallback: true);
                    break;
            }
        }

        private void DetachMode()
        {
            _avatarImage.image = null;

            if (_avatarService == null) return;

            switch (_mode)
            {
                case AvatarViewMode.FullBody:
                    s_ActiveFullBodyViewsCount = Mathf.Max(0, s_ActiveFullBodyViewsCount - 1);
                    if (s_ActiveFullBodyViewsCount == 0)
                    {
                        _avatarService.SetFullBodyCameraActive(false);
                    }
                    break;

                case AvatarViewMode.Bust:
                    s_ActiveBustViewsCount = Mathf.Max(0, s_ActiveBustViewsCount - 1);
                    if (s_ActiveBustViewsCount == 0)
                    {
                        _avatarService.SetAvatarCameraActive(false);
                    }
                    break;

                case AvatarViewMode.Face2D:
                    _avatarService.OnFaceTextureChanged -= HandleFaceTextureChanged;
                    break;
            }
        }

        private void HandleFaceTextureChanged()
        {
            if (_mode == AvatarViewMode.Face2D && _avatarService != null)
            {
                _avatarImage.image = _avatarService.GetFaceTexture(allowFallback: true);
            }
        }
    }
}
