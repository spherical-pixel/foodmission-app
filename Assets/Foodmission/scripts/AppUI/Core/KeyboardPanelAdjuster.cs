using System.Collections.Generic;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace eu.foodmission.platform
{
    /// <summary>
    /// Global keyboard adjuster that moves the entire UI Toolkit panel when keyboard appears.
    /// Attached to the main app and works for all screens automatically.
    /// </summary>
    public class KeyboardPanelAdjuster : MonoBehaviour
    {
        private IKeyboardService _keyboardService;
        private VisualElement _rootElement;

        private Vector3 _originalTranslate;
        private bool _hasStoredOriginalPosition;

        // Store the last focused element that might need keyboard adjustment
        private VisualElement _lastFocusedElement;
        private bool _isKeyboardVisible;

        [SerializeField]
        [Tooltip("Additional margin between focused field and keyboard (in pixels)")]
        private float keyboardMargin = 10f;

        [SerializeField]
        [Tooltip("Maximum percentage of screen to move (0-1)")]
        private float maxMovePercent = 0.5f;

        public void Initialize(VisualElement rootElement, IKeyboardService keyboardService)
        {
            _rootElement = rootElement;
            _keyboardService = keyboardService;

            if (_rootElement == null)
            {
                Debug.LogError("[KeyboardPanelAdjuster] Root element is null");
                return;
            }

            // Store original translate
            _originalTranslate = _rootElement.resolvedStyle.translate;
            _hasStoredOriginalPosition = true;

            // Subscribe to keyboard events
            if (_keyboardService != null)
            {
                _keyboardService.KeyboardShown += OnKeyboardShown;
                _keyboardService.KeyboardHidden += OnKeyboardHidden;
            }

            // Register for focus events globally
            _rootElement.RegisterCallback<FocusInEvent>(OnGlobalFocusIn, TrickleDown.TrickleDown);
        }

        private void OnDestroy()
        {
            if (_keyboardService != null)
            {
                _keyboardService.KeyboardShown -= OnKeyboardShown;
                _keyboardService.KeyboardHidden -= OnKeyboardHidden;
            }

            if (_rootElement != null)
            {
                _rootElement.UnregisterCallback<FocusInEvent>(OnGlobalFocusIn, TrickleDown.TrickleDown);
            }
        }

        private void OnGlobalFocusIn(FocusInEvent evt)
        {
            // Store the focused element - could be TextField, TextElement, etc.
            if (evt.target is VisualElement focusedElement)
            {
                // Check if this is a text input element (TextField or contains text input)
                if (IsTextInputElement(focusedElement))
                {
                    _lastFocusedElement = focusedElement;

                    // If keyboard is already visible, adjust immediately
                    if (_isKeyboardVisible && _keyboardService?.KeyboardHeight > 0)
                    {
                        _ = AdjustForKeyboardAsync(_keyboardService.KeyboardHeight);
                    }
                }
            }
        }

        /// <summary>
        /// Check if element is a text input element
        /// </summary>
        private bool IsTextInputElement(VisualElement element)
        {
            if (element == null) return false;

            // Check for TextField
            if (element is UnityEngine.UIElements.TextField) return true;

            // Check for AppUI TextField (which might wrap TextField)
            if (element.GetType().Name.Contains("TextField")) return true;

            // Check parent hierarchy for TextField (in case focus is on inner TextElement)
            var parent = element.parent;
            while (parent != null)
            {
                if (parent is UnityEngine.UIElements.TextField) return true;
                if (parent.GetType().Name.Contains("TextField")) return true;
                parent = parent.parent;
            }

            return false;
        }

        /// <summary>
        /// Called when keyboard is shown - this is the trigger for adjustment
        /// </summary>
        private void OnKeyboardShown(float height)
        {
            _isKeyboardVisible = true;

            if (height > 0 && _lastFocusedElement != null)
            {
                _ = AdjustForKeyboardAsync(height);
            }
        }

        private void OnKeyboardHidden()
        {
            _isKeyboardVisible = false;
            _lastFocusedElement = null;
            RestorePanelPosition();
        }

        private async System.Threading.Tasks.Task AdjustForKeyboardAsync(float keyboardHeight)
        {
            if (_rootElement == null || _lastFocusedElement == null)
            {
                return;
            }

            // Small delay to let keyboard finish appearing
            await System.Threading.Tasks.Task.Delay(100);

            if (_rootElement == null || _lastFocusedElement == null)
            {
                return;
            }

            // Calculate adjustment
            float offset = CalculatePanelOffset(_lastFocusedElement, keyboardHeight);

            if (offset != 0)
            {
                ApplyPanelOffset(offset);
            }
        }

        private float CalculatePanelOffset(VisualElement focusedElement, float keyboardHeight)
        {
            if (_rootElement == null || focusedElement == null)
            {
                return 0f;
            }

            // Get the current panel translate
            Vector3 panelTranslate = _rootElement.resolvedStyle.translate;

            // Get focused element bounds in screen space
            Rect elementBounds = focusedElement.worldBound;

            // Panel height
            float panelHeight = _rootElement.layout.height;
            if (panelHeight <= 0) panelHeight = Screen.height;

            // Scale factor (UI Toolkit units to screen pixels)
            float scale = _rootElement.layout.width / Screen.width;
            if (scale <= 0) scale = 1f;

            // Convert keyboard height from screen pixels to UI Toolkit units
            float keyboardHeightUI = keyboardHeight * scale;

            // Calculate where the keyboard top is in UI Toolkit coordinates
            // Keyboard appears at bottom of screen, so its top is at (panelHeight - keyboardHeight)
            float keyboardTopY = panelHeight - keyboardHeightUI - panelTranslate.y;

            // Element bottom in panel coordinates
            float elementBottomY = elementBounds.yMax;

            // Check if element is covered by keyboard
            float overlap = elementBottomY - keyboardTopY + (keyboardMargin * scale);

            if (overlap <= 0)
            {
                // Element is above keyboard
                return 0f;
            }

            // Cap the movement
            float maxOffset = panelHeight * maxMovePercent * scale;
            if (overlap > maxOffset)
            {
                overlap = maxOffset;
            }

            return -overlap; // Negative to move up
        }

        private void ApplyPanelOffset(float offset)
        {
            if (_rootElement == null || !_hasStoredOriginalPosition)
            {
                return;
            }

            // Use style.translate instead of obsolete transform.position
            var newTranslate = new Translate(_originalTranslate.x, _originalTranslate.y + offset, _originalTranslate.z);
            _rootElement.style.translate = newTranslate;
        }

        private void RestorePanelPosition()
        {
            if (_rootElement == null || !_hasStoredOriginalPosition)
            {
                return;
            }

            // Restore original translate
            _rootElement.style.translate = new Translate(_originalTranslate.x, _originalTranslate.y, _originalTranslate.z);
        }
    }
}
