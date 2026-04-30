using System;
using Unity.AppUI.Core;
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
            Action onOk = null,
            string koLabel = null,
            Action onKo = null
            )
        {
            var dialog = new AlertDialog
            {
                title = title,
                description = message,
                variant = semantic
            };

            dialog.SetPrimaryAction(0, okLabel, onOk ?? (() => { }));
            if( !string.IsNullOrEmpty(koLabel) && onKo != null)
            {
                dialog.SetCancelAction(1, koLabel);
                dialog.cancelButton.clicked += () => onKo?.Invoke();
            }
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

            // SetCancelAction only accepts (actionId, displayText) — no callback overload.
            // The cancel callback is captured via the modal's dismissed event instead.
            dialog.SetCancelAction(1, cancelLabel);

            var modal = Modal.Build(anchor, dialog);
            if (onCancel != null)
            {
                modal.dismissed += (_, dismissType) =>
                {
                    if (dismissType == DismissType.Manual)
                    {
                        onCancel.Invoke();
                    }
                };
            }

            modal.Show();
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
            // Use AlertDialog so SetPrimaryAction and SetCancelAction are available.
            // The scrollable text content is added directly to the dialog's contentContainer.
            var dialog = new AlertDialog { title = title };

            var scrollView = new ScrollView();
            scrollView.AddToClassList("fm-dialog-scroll");

            var text = new Unity.AppUI.UI.Text { text = content };
            text.AddToClassList("fm-dialog-scroll-text");

            scrollView.Add(text);
            dialog.contentContainer.Add(scrollView);

            dialog.SetPrimaryAction(0, acceptLabel, onAccept ?? (() => { }));

            // SetCancelAction only accepts (actionId, displayText); cancel callback via dismissed event.
            dialog.SetCancelAction(1, cancelLabel);

            var modal = Modal.Build(anchor, dialog);
            if (onCancel != null)
            {
                modal.dismissed += (_, dismissType) =>
                {
                    if (dismissType == DismissType.Manual)
                    {
                        onCancel.Invoke();
                    }
                };
            }

            modal.Show();
        }

        public static void ShowCustom(
            VisualElement anchor,
            string title,
            VisualElement content,
            params FMDialogAction[] actions)
        {
            if (actions == null || actions.Length == 0)
            {
                throw new ArgumentException("ShowCustom requires at least one action.", nameof(actions));
            }

            // Dialog does not expose SetPrimaryAction / SetCancelAction — those belong to AlertDialog.
            // We add Button instances directly to the public actionContainer instead.
            var dialog = new Dialog { title = title };
            dialog.contentContainer.Add(content);

            Modal modal = null;

            foreach (var action in actions)
            {
                var captured = action;
                var button = new Unity.AppUI.UI.Button
                {
                    title = captured.Label
                };

                if (captured.IsPrimary)
                {
                    button.variant = ButtonVariant.Accent;
                }

                button.clicked += () =>
                {
                    captured.Callback?.Invoke();
                    modal?.Dismiss(DismissType.Action);
                };

                dialog.actionContainer.Add(button);
            }

            modal = Modal.Build(anchor, dialog);
            modal.Show();
        }
    }
}
