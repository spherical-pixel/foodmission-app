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
        private System.Threading.CancellationTokenSource _adjustCts;

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
            _adjustCts = new System.Threading.CancellationTokenSource();

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

            _adjustCts?.Cancel();
            _adjustCts?.Dispose();
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

                    // Always attempt adjustment on focus change.
                    // If keyboard isn't visible yet (first tap), AdjustForKeyboardAsync
                    // will wait until TouchScreenKeyboard.visible becomes true.
                    float height = _keyboardService?.KeyboardHeight ?? 0f;
                    _ = AdjustForKeyboardAsync(height);
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

            // Cancel any previous pending adjustment
            _adjustCts?.Cancel();
            _adjustCts = new System.Threading.CancellationTokenSource();
            var token = _adjustCts.Token;

            // If keyboard isn't visible yet (first tap on iOS), wait for it to appear
            if (!TouchScreenKeyboard.visible)
            {
                try
                {
                    // Poll up to 2 seconds for keyboard to appear
                    for (int i = 0; i < 120; i++)
                    {
                        await System.Threading.Tasks.Task.Delay(16, token);
                        if (TouchScreenKeyboard.visible) break;
                    }
                }
                catch (System.Threading.Tasks.TaskCanceledException)
                {
                    return;
                }
            }

            // Also wait for keyboard height to stabilize (300ms for iOS animation)
            float height = keyboardHeight > 0 ? keyboardHeight : (_keyboardService?.KeyboardHeight ?? 0f);
            if (height <= 0)
            {
                try
                {
                    await System.Threading.Tasks.Task.Delay(300, token);
                    height = _keyboardService?.KeyboardHeight ?? 0f;
                }
                catch (System.Threading.Tasks.TaskCanceledException)
                {
                    return;
                }
            }
            else
            {
                try
                {
                    await System.Threading.Tasks.Task.Delay(300, token);
                }
                catch (System.Threading.Tasks.TaskCanceledException)
                {
                    return;
                }
            }

            if (token.IsCancellationRequested || _rootElement == null || _lastFocusedElement == null)
            {
                return;
            }

            // Re-read height in case it settled differently
            float finalHeight = _keyboardService?.KeyboardHeight ?? height;

            // Calculate adjustment
            float offset = CalculatePanelOffset(_lastFocusedElement, finalHeight);

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

            float panelHeight = _rootElement.layout.height;
            if (panelHeight <= 0) panelHeight = Screen.height;

            float scale = _rootElement.layout.width / Screen.width;
            if (scale <= 0) scale = 1f;

            float keyboardHeightUI = keyboardHeight * scale;

            // Keyboard top in panel coordinates (top-left origin).
            // On iOS, the keyboard sits above the home indicator (safe area bottom inset),
            // so we offset from the safe area bottom rather than the screen edge.
            // On devices without bottom inset (Screen.safeArea.y == 0), this matches
            // the original panelHeight - keyboardHeightUI formula.
            float keyboardBottomUI = (Screen.height - Screen.safeArea.y) * scale;
            float keyboardTopY = keyboardBottomUI - keyboardHeightUI;

            // Get element's current visual bottom (worldBound includes all parent transforms
            // including the panel's current translate and any scroll offsets)
            float elementBottom = focusedElement.worldBound.yMax;

            // If element is already above keyboard (with margin), no adjustment needed
            if (elementBottom <= keyboardTopY - (keyboardMargin * scale))
            {
                return 0f;
            }

            // Compute how much extra upward movement is needed beyond current translate
            Vector3 panelTranslate = _rootElement.resolvedStyle.translate;
            float extraNeeded = keyboardTopY - elementBottom - (keyboardMargin * scale);

            float finalTranslateY = panelTranslate.y + extraNeeded;

            // Cap max movement to prevent panel from going too far off-screen
            float maxMove = panelHeight * maxMovePercent * scale;
            if (finalTranslateY < -maxMove)
            {
                finalTranslateY = -maxMove;
            }

            return finalTranslateY - _originalTranslate.y;
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
