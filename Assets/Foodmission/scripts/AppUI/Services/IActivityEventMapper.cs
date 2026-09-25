using System;

namespace eu.foodmission.platform
{
    public enum DirectQuestionType
    {
        CountStepper,      // Numeric stepper (- / +)
        SingleChoice,      // Confirmation button / Yes-No
        SwapSelector,      // Dropdown / selection of specific food swaps
        OptionChips        // Multiple selectable option tags
    }

    [Serializable]
    public class ActivityMapping
    {
        public string ActivityCode { get; set; }
        public string[] TargetEventTypes { get; set; } = Array.Empty<string>();
        public string NativeModuleAction { get; set; } = "";
        public string NativeModuleLabelKey { get; set; } = "@UI:GO_TO_MODULE";
        public string NativeModuleExplanationKey { get; set; } = "@UI:ACTIVITY_NATIVE_MODULE_HINT";
        public string NativeModuleButtonTitle { get; set; } = "Ir al módulo";
        public string NativeModuleHint { get; set; } = "Registra esta acción en la app para acumular progreso.";
        public DirectQuestionType QuestionType { get; set; } = DirectQuestionType.SingleChoice;
        public string NutriPromptKey { get; set; } = "@UI:ACTIVITY_NUTRI_PROMPT";
        public string DirectQuestionPrompt { get; set; }
        public string[] SwapOptions { get; set; } = Array.Empty<string>();
        public int DefaultCount { get; set; } = 1;
        public int MaxCount { get; set; } = 7;
    }

    public interface IActivityEventMapper
    {
        ActivityMapping GetMissionMapping(string missionCode);
        ActivityMapping GetChallengeMapping(string challengeCode);
    }
}
