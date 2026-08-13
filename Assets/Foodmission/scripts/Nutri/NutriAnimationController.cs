using UnityEngine;

namespace eu.foodmission.platform
{
    public class NutriAnimationController : MonoBehaviour
    {
        private Animator _animator;

        private int _isTalkingHash = Animator.StringToHash("isTalking");
        private int _isGreetingHash = Animator.StringToHash("isGreeting");
        private int _isLookingDownHash = Animator.StringToHash("isLookingDown");
        private int _isCelebratingHash = Animator.StringToHash("isCelebrating");
        private int _moodHash = Animator.StringToHash("mood");

        private NutriMood _currentMood = NutriMood.Neutral;
        private NutriAction _currentAction = NutriAction.Idle;

        public NutriMood CurrentMood
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

        public NutriAction CurrentAction
        {
            get => _currentAction;
            set
            {
                if (_animator == null)
                {
                    Debug.LogError($"[{GetType().Name}] Animator component not found on GameObject '{gameObject.name}'");
                    return;
                }

                if (_currentAction != value)
                {
                    _currentAction = value;
                    _animator.SetBool(_isTalkingHash, _currentAction == NutriAction.Talking);
                    _animator.SetBool(_isGreetingHash, _currentAction == NutriAction.Greeting);
                    _animator.SetBool(_isLookingDownHash, _currentAction == NutriAction.LookingDown);
                    _animator.SetBool(_isCelebratingHash, _currentAction == NutriAction.Celebration);
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
            _animator.SetBool(_isTalkingHash, false);
            _animator.SetBool(_isGreetingHash, false);
            _animator.SetBool(_isLookingDownHash, false);
            _animator.SetBool(_isCelebratingHash, false);
        }
    }
}
