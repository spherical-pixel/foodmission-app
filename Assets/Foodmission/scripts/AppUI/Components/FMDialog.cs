using System;
using Unity.AppUI.Core;
using Unity.AppUI.UI;

using UnityEngine;
using UnityEngine.Accessibility;
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

            dialog.primaryButton.AddToClassList("fm-button");
            dialog.primaryButton.variant = ButtonVariant.Accent;

            dialog.SetPrimaryAction(0, okLabel, onOk ?? (() => { }));
            if( !string.IsNullOrEmpty(koLabel) && onKo != null)
            {
                dialog.SetCancelAction(1, koLabel);
                dialog.cancelButton.clicked += () => onKo?.Invoke();
                dialog.cancelButton.AddToClassList("fm-button");
                dialog.cancelButton.variant = ButtonVariant.Accent;
            }
            var modal = Modal.Build(anchor, dialog);
            NotifyScreenReaderOfDialog(modal, title, message);
            modal.Show();
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
            dialog.primaryButton.variant = ButtonVariant.Accent;

            // SetCancelAction only accepts (actionId, displayText) — no callback overload.
            // The cancel callback is captured via the modal's dismissed event instead.
            dialog.SetCancelAction(1, cancelLabel);
            dialog.cancelButton.variant = ButtonVariant.Accent;
            dialog.primaryButton.AddToClassList("fm-button");
            dialog.cancelButton.AddToClassList("fm-button");

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

            NotifyScreenReaderOfDialog(modal, title, message);
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
            dialog.primaryButton.AddToClassList("fm-button");
            dialog.primaryButton.variant = ButtonVariant.Accent;

            // SetCancelAction only accepts (actionId, displayText); cancel callback via dismissed event.
            dialog.SetCancelAction(1, cancelLabel);
            dialog.cancelButton.AddToClassList("fm-button");
            dialog.cancelButton.variant = ButtonVariant.Accent;

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

            NotifyScreenReaderOfDialog(modal, title, content);
            modal.Show();
        }

        public static void ShowApiError(
            VisualElement anchor,
            string title,
            ApiErrorResponse error,
            string okLabel = "@UI:TXT_OK")
        {
            string message = error?.message ?? "Unknown error";
            string traceInfo = !string.IsNullOrEmpty(error?.traceId)
                ? $"\n\nTrace ID: {error.traceId}"
                : "";

            ShowAlert(anchor, title, $"{message}{traceInfo}", AlertSemantic.Error, okLabel);
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
            NotifyScreenReaderOfDialog(modal, title, "");
            modal.Show();
        }

        /// <summary>
        /// Show a large, scrollable info popup: a heading, a scrollable text body,
        /// and one or more action buttons. The modal content card is sized as a
        /// percentage of the screen (default 80% × 60%).
        /// </summary>
        /// <param name="anchor">Reference view for the Modal (e.g. a screen's contentContainer).</param>
        /// <param name="title">Heading text.</param>
        /// <param name="body">Body text rendered inside a ScrollView. Null is coerced to empty.</param>
        /// <param name="actions">Buttons. Must contain at least one action.</param>
        /// <param name="widthPercent">Modal card width as % of screen (default 80).</param>
        /// <param name="heightPercent">Modal card height as % of screen (default 60).</param>
        public static void ShowInfo(
            VisualElement anchor,
            string title,
            string body,
            FMDialogAction[] actions,
            float widthPercent = 80f,
            float heightPercent = 60f)
        {
            if (actions == null || actions.Length == 0)
            {
                throw new ArgumentException("ShowInfo requires at least one action.", nameof(actions));
            }

            var root = new VisualElement();
            root.AddToClassList("fm-info-dialog");

            var heading = new Heading(title) { size = HeadingSize.XL };
            heading.AddToClassList("fm-info-dialog__heading");
            root.Add(heading);

            var scrollView = new ScrollView();
            scrollView.AddToClassList("fm-info-dialog__scroll");

            var bodyText = new Unity.AppUI.UI.Text(body ?? "") { size = TextSize.M };
            bodyText.AddToClassList("fm-info-dialog__body");
            scrollView.Add(bodyText);
            root.Add(scrollView);

            var actionsRow = new VisualElement();
            actionsRow.AddToClassList("fm-info-dialog__actions");

            Modal modal = null;

            for (int i = 0; i < actions.Length; i++)
            {
                var action = actions[i];

                if (i > 0)
                {
                    actionsRow.Add(new Spacer { spacing = SpacerSpacing.M });
                }

                var button = new Unity.AppUI.UI.Button
                {
                    title = action.Label,
                    variant = action.IsPrimary ? ButtonVariant.Accent : ButtonVariant.Default
                };
                button.AddToClassList("fm-button");
                button.style.flexGrow = 1;

                var captured = action;
                button.clicked += () =>
                {
                    captured.Callback?.Invoke();
                    modal?.Dismiss(DismissType.Action);
                };

                actionsRow.Add(button);
            }

            root.Add(actionsRow);

            modal = Modal.Build(anchor, root);
            modal.SetFullScreenMode(ModalFullScreenMode.None);

            var modalContent = modal.view.contentContainer;
            modalContent.style.width = Length.Percent(widthPercent);
            modalContent.style.height = Length.Percent(heightPercent);

            NotifyScreenReaderOfDialog(modal, title, body ?? "");
            modal.Show();
        }

        private static void NotifyScreenReaderOfDialog(Modal modal, string title, string content)
        {
            if (!AssistiveSupport.isScreenReaderEnabled) return;

            var dispatcher = AssistiveSupport.notificationDispatcher;
            if (dispatcher == null) return;

            dispatcher.SendLayoutChanged();

            modal.dismissed += (_, _) =>
            {
                dispatcher.SendLayoutChanged();
            };
        }
    }
}
