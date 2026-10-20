using System.Collections.Generic;
using System.Threading.Tasks;

using eu.foodmission.platform.Components;

using Unity.AppUI.MVVM;
using Unity.AppUI.UI;

using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;

namespace eu.foodmission.platform
{
    /// <summary>Tutorial bubbles with <see cref="NutriMessageDialog"/>: Skip/Next between steps, a single "Got it" for one step.</summary>
    public sealed class NutriTutorialPresenter : ITutorialPresenter
    {
        // App UI Modal's element class (Modal.ModalVisualElement.ussClassName is private)
        private const string k_ModalClassName = "appui-modal";

        public bool IsAnyDialogOpen => App.current?.rootVisualElement?.panel?.visualTree?.Q(className: k_ModalClassName) != null;

        public Task<bool> ShowAsync(IReadOnlyList<string> messageKeys)
        {
            // Asynchronous continuations: the caller never resumes inside the dialog's dismiss handler
            var result = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            if (messageKeys == null || messageKeys.Count == 0 || App.current?.rootVisualElement == null)
            {
                result.TrySetResult(false);
                return result.Task;
            }

            ShowStep(messageKeys, 0, result);
            return result.Task;
        }

        private static void ShowStep(IReadOnlyList<string> keys, int index, TaskCompletionSource<bool> result)
        {
            bool isLast = index == keys.Count - 1;
            bool next = false;
            string message = L(keys[index]);
            void OnDismissed()
            {
                if (next)
                {
                    ShowStep(keys, index + 1, result);
                }
                else
                {
                    result.TrySetResult(true);
                }
            }

            if (keys.Count == 1)
            {
                NutriMessageDialog.ShowAndNotify(message, OnDismissed,
                    new FMDialogAction(L(SectionTutorials.GotItKey), null, ButtonVariant.Accent));
            }
            else if (isLast)
            {
                NutriMessageDialog.ShowAndNotify(message, OnDismissed,
                    new FMDialogAction(L(SectionTutorials.StartKey), null, ButtonVariant.Accent));
            }
            else
            {
                NutriMessageDialog.ShowAndNotify(message, OnDismissed,
                    new FMDialogAction(L(SectionTutorials.SkipKey), null),
                    new FMDialogAction(L(SectionTutorials.NextKey), () => next = true, ButtonVariant.Accent));
            }
        }

        private static string L(string key)
        {
            return LocalizationSettings.StringDatabase.GetLocalizedString("UI", key);
        }
    }
}
