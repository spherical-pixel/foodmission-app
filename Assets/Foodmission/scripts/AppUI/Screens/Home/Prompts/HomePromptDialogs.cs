using System.Threading.Tasks;

using eu.foodmission.platform.Components;

using Unity.AppUI.UI;

using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;

namespace eu.foodmission.platform
{
    public readonly struct PromptChoice
    {
        public readonly string Label;
        public readonly ButtonVariant Variant;

        public PromptChoice(string label, ButtonVariant variant)
        {
            Label = label;
            Variant = variant;
        }
    }

    /// <summary>Home prompt dialogs as tasks that complete when the dialog closes by any path.</summary>
    public static class HomePromptDialogs
    {
        public const int Closed = -1;

        /// <summary>Index of the pressed choice, or <see cref="Closed"/> when closed without a button.</summary>
        public static Task<int> ChooseNutriAsync(string message, params PromptChoice[] choices)
        {
            // Asynchronous continuations: the caller never resumes (and opens the next dialog) inside this dialog's dismiss handler
            var result = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            int pressed = Closed;
            NutriMessageDialog.ShowAndNotify(message, () => result.TrySetResult(pressed), ToActions(choices, i => pressed = i));
            return result.Task;
        }

        public static Task<int> ChooseInfoAsync(VisualElement anchor, string title, string body, params PromptChoice[] choices)
        {
            var result = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            int pressed = Closed;
            FMDialog.ShowInfo(anchor, title, body, ToActions(choices, i => pressed = i), () => result.TrySetResult(pressed));
            return result.Task;
        }

        /// <summary>True when accepted; false when declined or closed.</summary>
        public static Task<bool> ShowDocumentAsync(VisualElement anchor, string title, string markdown)
        {
            var result = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            // The accept callback runs before the dismiss event, so closing by any other path resolves false
            FMDialog.ShowScrollableMD(anchor, title, markdown,
                onAccept: () => result.TrySetResult(true),
                onDismissed: () => result.TrySetResult(false));
            return result.Task;
        }

        public static Task ShowApiErrorAsync(VisualElement anchor, ApiErrorResponse error)
        {
            var result = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            FMDialog.ShowApiError(anchor, LocalizationSettings.StringDatabase.GetLocalizedString("UI", "ERROR_TITLE"), error,
                onOk: () => result.TrySetResult(true), onDismissed: () => result.TrySetResult(true));
            return result.Task;
        }

        private static FMDialogAction[] ToActions(PromptChoice[] choices, System.Action<int> onPressed)
        {
            var actions = new FMDialogAction[choices.Length];
            for (int i = 0; i < choices.Length; i++)
            {
                int index = i;
                actions[i] = new FMDialogAction(choices[i].Label, () => onPressed(index), choices[i].Variant);
            }
            return actions;
        }
    }
}
