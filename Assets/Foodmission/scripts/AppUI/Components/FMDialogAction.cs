using System;

namespace eu.foodmission.platform.Components
{
    public struct FMDialogAction
    {
        public string Label;
        public Action Callback;
        public bool IsPrimary;

        public FMDialogAction(string label, Action callback, bool isPrimary = false)
        {
            Label = label;
            Callback = callback;
            IsPrimary = isPrimary;
        }
    }
}
