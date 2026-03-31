using System;

using Unity.AppUI.UI;

using UnityEngine.UIElements;

namespace eu.foodmission.platform
{
    /// <summary>
    /// Reusable notification card component.
    /// Instantiated from NotificationCard.uxml and populated via Bind().
    /// </summary>
    public class NotificationCard : VisualElement
    {
        private Label _textLabel;
        private Label _timestampLabel;
        private ActionButton _overflowBtn;
        private string _currentId;

        public event Action<string> OnView;
        public event Action<string> OnDelete;

        public NotificationCard(VisualTreeAsset template)
        {
            template.CloneTree(this);
            AddToClassList("fm-notification-card-wrapper");

            _textLabel      = this.Q<Label>("text");
            _timestampLabel = this.Q<Label>("timestamp");
            _overflowBtn    = this.Q<ActionButton>("overflow-btn");

            if (_overflowBtn != null)
            {
                _overflowBtn.clicked += OnOverflowClicked;
            }
        }

        /// <summary>
        /// Populates the card with data from the given model.
        /// Can be called multiple times to rebind.
        /// </summary>
        public void Bind(NotificationModel model)
        {
            if (model == null)
            {
                return;
            }

            _currentId = model.Id;

            if (_textLabel != null)
            {
                _textLabel.text = model.Text;
            }

            if (_timestampLabel != null)
            {
                _timestampLabel.text = model.Timestamp;
            }

            EnableInClassList("fm-notification-card--read", model.IsRead);
        }

        private void OnOverflowClicked()
        {
            if (_currentId == null)
            {
                return;
            }
            OnDelete?.Invoke(_currentId);
        }
    }
}
