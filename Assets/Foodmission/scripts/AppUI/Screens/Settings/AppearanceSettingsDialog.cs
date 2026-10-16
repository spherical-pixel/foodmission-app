using System;

using Unity.AppUI.MVVM;
using Unity.AppUI.UI;

using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.UIElements;
using eu.foodmission.platform.Components;

namespace eu.foodmission.platform
{
    /// <summary>
    /// Modal with the appearance settings (language, text size, font, theme, background) for the screens
    /// before login (Login, Register). Changes apply at once through SettingsViewModel; without a session
    /// they stay local and are sent to the backend after registering.
    /// </summary>
    public static class AppearanceSettingsDialog
    {
        /// <summary>Builds the panel's steppers. Exposed for tests.</summary>
        public static VisualElement BuildPanel(out AppearanceSettingsBinder binder)
        {
            var panel = new VisualElement { name = "appearance-settings-panel" };
            panel.AddToClassList("fm-appearance-settings");

            FormFieldItemArrowStepperSettings lang = AddStepper(panel, "stepper-lang", "@UI:LANGUAGE");
            FormFieldItemArrowStepperSettings scale = AddStepper(panel, "stepper-scale", "@UI:TEXT_SIZE");
            FormFieldItemArrowStepperSettings font = AddStepper(panel, "stepper-font", "@UI:FONT");
            FormFieldItemArrowStepperSettings theme = AddStepper(panel, "stepper-theme", "@UI:THEME");
            FormFieldItemArrowStepperSettings background = AddStepper(panel, "stepper-background", "@UI:BACKGROUND");

            binder = new AppearanceSettingsBinder(scale, theme, background, font, lang);
            return panel;
        }

        /// <summary>Destinations that show the gear button (Login has its own in the UXML, Register gets it in the AppBar).</summary>
        public static bool IsShownInAppBar(string destinationName) =>
            destinationName == Unity.AppUI.Navigation.Generated.Destinations.register;

        /// <param name="anchor">Reference view for the Modal (a screen's contentContainer or the AppBar).</param>
        public static void Show(VisualElement anchor)
        {
            SettingsViewModel viewModel;
            try
            {
                viewModel = App.current.services.GetRequiredService<SettingsViewModel>();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AppearanceSettingsDialog] Could not create SettingsViewModel: {ex.Message}");
                return;
            }

            VisualElement panel = BuildPanel(out AppearanceSettingsBinder binder);
            binder.Bind(viewModel);

            // The screen's hierarchy (set by NavigationScreenBase); null when no screen reader is active
            AccessibilityHierarchy hierarchy = AssistiveSupport.activeHierarchy;
            binder.CreateAccessibilityNodes(hierarchy);
            NotifyLayoutChanged(hierarchy);

            FMDialog.ShowCustom(
                anchor,
                "@UI:SETTINGS",
                panel,
                onDismissed: () =>
                {
                    binder.DestroyAccessibilityNodes();
                    NotifyLayoutChanged(hierarchy);
                    binder.Unbind();
                    viewModel.Dispose();
                },
                new FMDialogAction("@UI:TXT_DONE", null, ButtonVariant.Accent));
        }

        private static void NotifyLayoutChanged(AccessibilityHierarchy hierarchy)
        {
            if (hierarchy != null)
            {
                AssistiveSupport.notificationDispatcher?.SendLayoutChanged();
            }
        }

        private static FormFieldItemArrowStepperSettings AddStepper(VisualElement panel, string name, string heading)
        {
            var stepper = new FormFieldItemArrowStepperSettings { name = name, HeadingText = heading };
            panel.Add(stepper);
            return stepper;
        }
    }
}
