using System;
using System.Linq;
using System.Threading.Tasks;

using Unity.AppUI.UI;

using UnityEngine;
using UnityEngine.Localization.Settings;

using eu.foodmission.platform.Components;

namespace eu.foodmission.platform
{
    public readonly struct MissionInputsAnswer
    {
        /// <summary>False when the user cancelled: start/restart nothing.</summary>
        public readonly bool Proceed;
        public readonly int? PreviousMeatMeals;

        public MissionInputsAnswer(bool proceed, int? previousMeatMeals)
        {
            Proceed = proceed;
            PreviousMeatMeals = previousMeatMeals;
        }
    }

    /// <summary>
    /// Nutri's question before starting or restarting a mission whose rule takes inputs (M.B1.3: meals with meat last
    /// week, prefilled from the meal logs). Shared by mission detail, the quest timeline and Home's failed-mission notice.
    /// </summary>
    public static class MissionInputsDialog
    {
        /// <param name="suggest">Initial value of the stepper (meat meals logged in the last 7 days).</param>
        /// <param name="confirmKey">UI.csv key of the confirm button (MISSION_START or MISSION_BTN_RESTART).</param>
        public static async Task<MissionInputsAnswer> AskAsync(string missionCode, Func<Task<int>> suggest, string confirmKey)
        {
            if (!MissionInputs.AsksPreviousMeatMeals(missionCode))
            {
                return new MissionInputsAnswer(true, null);
            }

            int initial = 0;
            try
            {
                initial = suggest != null ? await suggest() : 0;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MissionInputsDialog] suggestion error: {ex.Message}");
            }

            // A count: 21 must not wrap around to 0
            var stepper = new FMArrowStepper
            {
                Cyclic = false,
                Choices = Enumerable.Range(0, MissionInputs.MaxPreviousMeatMeals + 1).Select(n => n.ToString()).ToArray()
            };
            stepper.SelectedIndex = Math.Clamp(initial, 0, MissionInputs.MaxPreviousMeatMeals);

            // Asynchronous continuations: the caller resumes after the dialog has closed, not inside its handler
            var result = new TaskCompletionSource<MissionInputsAnswer>(TaskCreationOptions.RunContinuationsAsynchronously);
            int? answer = null;
            NutriMessageDialog.ShowWithContent(
                Localize("MISSION_START_MEAT_QUESTION"),
                stepper,
                () => result.TrySetResult(new MissionInputsAnswer(answer.HasValue, answer)),
                new FMDialogAction(Localize(confirmKey), () => answer = stepper.SelectedIndex, ButtonVariant.Accent),
                new FMDialogAction(Localize("TXT_CANCEL"), () => { }));
            return await result.Task;
        }

        /// <summary>
        /// "Start «title»? Its 7 days begin now." for a start that has no button text of its own (the quest timeline's
        /// play icon). True when confirmed; false when cancelled or closed.
        /// </summary>
        public static Task<bool> ConfirmStartAsync(string missionTitle)
        {
            return ConfirmAsync("MISSION_START_CONFIRM", missionTitle, "MISSION_START");
        }

        /// <summary>"Restart «title»? A new 7-day period starts now." before restarting a failed mission.</summary>
        public static Task<bool> ConfirmRestartFailedAsync(string missionTitle)
        {
            return ConfirmAsync("MISSION_RESTART_FAILED_CONFIRM", missionTitle, "MISSION_BTN_RESTART");
        }

        private static Task<bool> ConfirmAsync(string messageKey, string missionTitle, string confirmKey)
        {
            var result = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            bool confirmed = false;
            NutriMessageDialog.ShowAndNotify(
                LocalizationSettings.StringDatabase.GetLocalizedString("UI", messageKey, new object[] { missionTitle }),
                () => result.TrySetResult(confirmed),
                new FMDialogAction(Localize(confirmKey), () => confirmed = true, ButtonVariant.Accent),
                new FMDialogAction(Localize("TXT_CANCEL"), () => { }));
            return result.Task;
        }

        /// <summary>After starting M.B1.3 with 0: congratulates and explains the meat-free week.</summary>
        public static Task ShowMeatFreeGoalAsync()
        {
            var result = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            NutriMessageDialog.ShowAndNotify(
                Localize("MISSION_START_MEAT_NONE"),
                () => result.TrySetResult(true),
                new FMDialogAction(Localize("TXT_OK"), () => { }, ButtonVariant.Accent));
            return result.Task;
        }

        private static string Localize(string key)
        {
            return LocalizationSettings.StringDatabase.GetLocalizedString("UI", key);
        }
    }
}
