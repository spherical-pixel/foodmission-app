using UnityEngine;

public class RandomAnimationBehaviour : StateMachineBehaviour
{
    [SerializeField] private string[] animationClips;
    [Tooltip("Duration of transition in seconds (fade duration).")]
    [SerializeField] private float transitionDuration = 0.25f;
    [Tooltip("If true, duration is measured in seconds. If false, normalized time (0 to 1).")]
    [SerializeField] private bool useFixedTime = true;

    private int _lastTriggeredFrame = -1;

    override public void OnStateMachineEnter(Animator animator, int stateMachinePathHash)
    {
        TriggerRandomAnimation(animator);
    }

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // If already triggered this frame (e.g. via OnStateMachineEnter), ignore
        if (Time.frameCount == _lastTriggeredFrame) return;

        // If the state that is entering is already one of the target animation clips, ignore to prevent infinite loops
        if (animationClips != null)
        {
            foreach (var clip in animationClips)
            {
                if (!string.IsNullOrEmpty(clip) && stateInfo.IsName(clip))
                {
                    return;
                }
            }
        }

        TriggerRandomAnimation(animator);
    }

    private void TriggerRandomAnimation(Animator animator)
    {
        if (animationClips == null || animationClips.Length == 0) return;
        if (Time.frameCount == _lastTriggeredFrame) return;
        _lastTriggeredFrame = Time.frameCount;

        int randomIndex = Random.Range(0, animationClips.Length);
        string targetClip = animationClips[randomIndex];

        if (string.IsNullOrEmpty(targetClip)) return;

        if (transitionDuration > 0f)
        {
            if (useFixedTime)
            {
                animator.CrossFadeInFixedTime(targetClip, transitionDuration);
            }
            else
            {
                animator.CrossFade(targetClip, transitionDuration);
            }
        }
        else
        {
            animator.Play(targetClip);
        }
    }
}


