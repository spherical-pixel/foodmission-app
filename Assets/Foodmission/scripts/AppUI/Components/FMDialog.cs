using System;

using Unity.AppUI.UI;

using UnityEngine.UIElements;

namespace eu.foodmission.platform.Components
{
    public static class FMDialog
    {
public static void ShowAlert(
            VisualElement anchor,
            string title,
            string message,
            AlertSemantic semantic = AlertSemantic.Information,
            string okLabel = "@UI:TXT_OK",
            Action onOk = null)
        {
            var dialog = new AlertDialog
            {
                title = title,
                description = message,
                variant = semantic
            };

            dialog.SetPrimaryAction(0, okLabel, onOk ?? (() => { }));

            Modal.Build(anchor, dialog).Show();
        }

public static void ShowConfirm(
            VisualElement anchor,
            string title,
            string message,
            Action onConfirm,
            Action onCancel = null,
            AlertSemantic semantic = AlertSemantic.Information,
            string confirmLabel = "@UI:TXT_ACCEPT",
            string cancelLabel = "@UI:TXT_CANCEL")
        {
            var dialog = new AlertDialog
            {
                title = title,
                description = message,
                variant = semantic
            };

            dialog.SetPrimaryAction(0, confirmLabel, onConfirm ?? (() => { }));
            dialog.SetCancelAction(1, cancelLabel, onCancel ?? (() => { }));

            Modal.Build(anchor, dialog).Show();
        }
    }
}
