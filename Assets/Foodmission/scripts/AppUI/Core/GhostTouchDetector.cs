using UnityEngine;

namespace eu.foodmission.platform
{
    /// <summary>
    /// Detects touches that iOS replays when the soft keyboard shows or hides (issue #27).
    /// The replay arrives as a new PointerDown/MouseDown at the exact position of the previous touch.
    /// Since <see cref="KeyboardPanelAdjuster"/> has moved the panel by then, it lands on another element,
    /// steals focus from the text field and closes the keyboard (and can loop forever).
    /// A real finger never repeats the exact same position right after a keyboard transition.
    /// </summary>
    public sealed class GhostTouchDetector
    {
        private const float PositionTolerance = 0.5f;
        private const float TransitionWindowSeconds = 1f;

        private Vector2? _lastPosition;
        private float _lastTransitionTime = float.NegativeInfinity;
        private int _lastPointerDownFrame = -1;
        private int _ghostFrame = -1;

        public void NotifyKeyboardTransition(float time)
        {
            _lastTransitionTime = time;
        }

        public void RecordPointerUp(Vector2 position)
        {
            _lastPosition = position;
        }

        public bool ShouldIgnorePointerDown(Vector2 position, float time, int frame)
        {
            if (IsReplay(position, time))
            {
                _ghostFrame = frame;
                return true;
            }

            _lastPosition = position;
            _lastPointerDownFrame = frame;
            return false;
        }

        public bool ShouldIgnoreMouseDown(Vector2 position, float time, int frame)
        {
            if (frame == _ghostFrame)
            {
                return true;
            }

            // Compatibility MouseDown sent for a real PointerDown in the same frame
            if (frame == _lastPointerDownFrame)
            {
                return false;
            }

            return IsReplay(position, time);
        }

        private bool IsReplay(Vector2 position, float time)
        {
            if (!_lastPosition.HasValue)
            {
                return false;
            }

            if (time - _lastTransitionTime > TransitionWindowSeconds)
            {
                return false;
            }

            return Vector2.Distance(position, _lastPosition.Value) <= PositionTolerance;
        }
    }
}
