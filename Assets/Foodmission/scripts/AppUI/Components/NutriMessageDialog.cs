using System;
using System.Linq;
using Unity.AppUI.Core;
using Unity.AppUI.MVVM;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UIElements;

namespace eu.foodmission.platform.Components
{
    public static class NutriMessageDialog
    {
        private static VisualTreeAsset _template;
        private static bool _templateLoading;

        private const string _addressablePath = "Foodmission/AppUI/Templates/NutriMessageWithButtons.uxml";

        private static void LoadTemplateAsync(Action<VisualTreeAsset> onLoaded)
        {
            if (_template != null)
            {
                onLoaded?.Invoke(_template);
                return;
            }

            if (_templateLoading)
            {
                return;
            }

            _templateLoading = true;
            Addressables.LoadAssetAsync<VisualTreeAsset>(_addressablePath).Completed += operation =>
            {
                _templateLoading = false;
                if (operation.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
                {
                    _template = operation.Result;
                    onLoaded?.Invoke(_template);
                }
                else
                {
                    Debug.LogError($"[NutriMessageDialog] Failed to load template at {_addressablePath}");
                    onLoaded?.Invoke(null);
                }
            };
        }

        public static void Show(
            string message,
            params FMDialogAction[] actions)
        {
            Show(message, null, actions);
        }

        public static void Show(
            string message,
            RenderTexture nutriTexture,
            params FMDialogAction[] actions)
        {
            if (actions == null || actions.Length == 0)
            {
                throw new ArgumentException("Show requires at least one action.", nameof(actions));
            }

            if (_template == null)
            {
                LoadTemplateAsync(template =>
                {
                    if (template != null)
                    {
                        Show(message, nutriTexture, actions);
                    }
                });
                return;
            }

            BuildAndShow(message, nutriTexture, actions);
        }

        private static void BuildAndShow(
            string message,
            RenderTexture nutriTexture,
            FMDialogAction[] actions)
        {
            var nutriService = App.current?.services?.GetService<INutriService>();
            if (nutriService != null)
            {
                nutriService.SetActive(true);
                nutriService.SetCameraActive(true);
            }

            var root = _template.Instantiate();
            root.style.flexGrow = 1;
            root.AddToClassList("fm-nutri-dialog");

            var contentContainer = root.Q<VisualElement>("content-container");
            var messageContainer = root.Q<ExVisualElement>("message-container");
            var nutriImage = root.Q<Image>("nutri-image");
            var buttonsContainer = root.Q<ExVisualElement>("buttons-container");

            // Message
            if (messageContainer != null)
            {
                var text = new Unity.AppUI.UI.Text(message)
                {
                    primary = true,
                    size = TextSize.XL
                };
                messageContainer.Add(text);
            }

            // Nutri image
                if (nutriImage != null)
                {
                    if (nutriTexture != null)
                    {
                        nutriImage.image = nutriTexture;
                    }
                    else if (nutriService != null)
                    {
                        nutriImage.image = nutriService.NutriCameraRenderTexture;
                    }
                    nutriImage.style.display = DisplayStyle.Flex;
                }

            // Buttons
            Modal modal = null;

            for (int i = 0; i < actions.Length; i++)
            {
                var action = actions[i];

                if (i > 0)
                {
                    var spacer = new Spacer { spacing = SpacerSpacing.M };
                    buttonsContainer.Add(spacer);
                }

                var button = new Unity.AppUI.UI.Button
                {
                    title = action.Label,
                    variant = action.IsPrimary ? ButtonVariant.Accent : ButtonVariant.Default,
                    size = Size.L
                };

                if (action.IsPrimary)
                {
                    button.AddToClassList("fm-button");
                    button.AddToClassList("fm-button-align-left");
                    button.trailingIcon = "fm-arrow-right";
                }

                var capturedAction = action;
                button.clicked += () =>
                {
                    if (nutriService != null)
                    {
                        nutriService.SetCameraActive(false);
                    }

                    root.RemoveFromClassList("fm-nutri-dialog--visible");
                    root.AddToClassList("fm-nutri-dialog--exit");
                    root.schedule.Execute(() =>
                    {
                        capturedAction.Callback?.Invoke();
                        modal?.Dismiss(DismissType.Action);
                    }).StartingIn(200);
                };

                buttonsContainer.Add(button);
            }

            App.current?.services?.GetService<IThemeService>()?.ApplySafeAreaPadding(contentContainer,true,false,false,false);
             
            // Final spacer
            var finalSpacer = new Spacer { spacing = SpacerSpacing.XL };
            buttonsContainer.Add(finalSpacer);

            var panelRoot = App.current.rootVisualElement;
            modal = Modal.Build(panelRoot, root);
            modal.SetFullScreenMode(ModalFullScreenMode.FullScreenTakeOver);
            modal.Show();

            root.schedule.Execute(() => root.AddToClassList("fm-nutri-dialog--visible")).StartingIn(50);

        }
    }
}
