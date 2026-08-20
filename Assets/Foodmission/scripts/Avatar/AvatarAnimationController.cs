using UnityEngine;

namespace eu.foodmission.platform
{
    public class AvatarAnimationController : MonoBehaviour
    {
        private Animator _animator;



        private int _triggerCelebration = Animator.StringToHash("triggerCelebration");
        private int _moodHash = Animator.StringToHash("mood");

        private AvatarMood _currentMood = AvatarMood.Neutral;

        public AvatarMood CurrentMood
        {
            get => _currentMood;
            set
            {
                if (_animator == null)
                {
                    Debug.LogError($"[{GetType().Name}] Animator component not found on GameObject '{gameObject.name}'");
                    return;
                }

                if (_currentMood != value)
                {
                    _currentMood = value;
                    _animator.SetInteger(_moodHash, (int)_currentMood);
                }
            }
        }

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            if (_animator == null)
            {
                _animator = GetComponent<Animator>();
            }
            if (_animator == null)
            {
                Debug.LogError($"[{GetType().Name}] Animator component not found on GameObject '{gameObject.name}'");
            }

            _animator.SetInteger(_moodHash, (int)_currentMood);
        }

        public void TriggerCelebration()
        {
            _animator.SetTrigger(_triggerCelebration);
        }

        public void UpdateAnimationController(float deltaTime = 0.0f)
        {
            _animator.Update(deltaTime);
        }

    }
}
