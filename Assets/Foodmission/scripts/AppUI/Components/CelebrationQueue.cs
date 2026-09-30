using System.Collections.Generic;

namespace eu.foodmission.platform.Components
{
    /// <summary>
    /// Serializes full-screen celebrations: while one is showing, new ones wait instead of replacing it,
    /// so two rewards earned at the same time (e.g. a food fact and the challenge it completes) are both seen.
    /// </summary>
    public sealed class CelebrationQueue<T>
    {
        private readonly Queue<T> _pending = new Queue<T>();
        private bool _isShowing;

        /// <summary>True when the celebration can be shown now; false when it was queued.</summary>
        public bool TryBegin(T celebration)
        {
            if (_isShowing)
            {
                _pending.Enqueue(celebration);
                return false;
            }

            _isShowing = true;
            return true;
        }

        /// <summary>Call when the current celebration closes. Returns the next one to show, if any.</summary>
        public bool OnDismissed(out T next)
        {
            if (_pending.Count > 0)
            {
                next = _pending.Dequeue();
                return true;
            }

            _isShowing = false;
            next = default;
            return false;
        }
    }
}
