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

        public static void ShowScrollable(
            VisualElement anchor,
            string title,
            string content,
            Action onAccept = null,
            Action onCancel = null,
            string acceptLabel = "@UI:TXT_ACCEPT",
            string cancelLabel = "@UI:TXT_BACK")
        {
            var dialog = new Dialog { title = title };

            var scrollView = new ScrollView();
            scrollView.AddToClassList("fm-dialog-scroll");

            var text = new Text { text = content };
            text.AddToClassList("fm-dialog-scroll-text");

            scrollView.Add(text);
            dialog.Add(scrollView);

            dialog.SetPrimaryAction(0, acceptLabel, onAccept ?? (() => { }));
            dialog.SetCancelAction(1, cancelLabel, onCancel ?? (() => { }));

            Modal.Build(anchor, dialog).Show();
        }

        public static void ShowCustom(
            VisualElement anchor,
            string title,
            VisualElement content,
            params FMDialogAction[] actions)
        {
            var dialog = new Dialog { title = title };
            dialog.Add(content);

            int priority = 0;
            foreach (var action in actions)
            {
                var captured = action;
                if (captured.IsPrimary)
                {
                    dialog.SetPrimaryAction(priority++, captured.Label, captured.Callback ?? (() => { }));
                }
                else
                {
                    dialog.SetCancelAction(priority++, captured.Label, captured.Callback ?? (() => { }));
                }
            }

            Modal.Build(anchor, dialog).Show();
        }
    }
}
