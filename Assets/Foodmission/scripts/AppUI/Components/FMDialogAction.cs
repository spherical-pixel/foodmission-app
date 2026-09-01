using System;
using Unity.AppUI.UI;

namespace eu.foodmission.platform.Components
{
    public struct FMDialogAction
    {
        public string Label;
        public Action Callback;
        public ButtonVariant ButtonVariant;

        public FMDialogAction(string label, Action callback, ButtonVariant buttonVariant = ButtonVariant.Default)
        {
            Label = label;
            Callback = callback;
            ButtonVariant = buttonVariant;
        }
    }
}
