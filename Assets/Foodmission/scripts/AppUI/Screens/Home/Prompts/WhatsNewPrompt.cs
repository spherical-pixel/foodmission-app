using System;
using System.Threading.Tasks;

using Unity.AppUI.UI;

using UnityEngine;
using UnityEngine.Localization.Settings;

namespace eu.foodmission.platform
{
    /// <summary>Release notes of a new app version, once.</summary>
    public sealed class WhatsNewPrompt : IHomePrompt
    {
        private readonly IWhatsNewService _whatsNew;

        public WhatsNewPrompt(IWhatsNewService whatsNew)
        {
            _whatsNew = whatsNew;
        }

        public HomePromptKind Kind => HomePromptKind.Info;

        public async Task<IHomePromptInstance> CheckAsync()
        {
            if (_whatsNew == null)
            {
                return null;
            }

            var (shouldShow, notes) = await _whatsNew.CheckShouldShowAsync();
            return shouldShow ? new Instance(_whatsNew, notes) : null;
        }

        private sealed class Instance : IHomePromptInstance
        {
            private readonly IWhatsNewService _whatsNew;
            private readonly string _notes;

            public Instance(IWhatsNewService whatsNew, string notes)
            {
                _whatsNew = whatsNew;
                _notes = notes;
            }

            public async Task<HomePromptResult> ShowAsync(IHomePromptHost host)
            {
                await HomePromptDialogs.ChooseInfoAsync(
                    host.DialogAnchor,
                    LocalizationSettings.StringDatabase.GetLocalizedString("UI", "txtWhatsNew", new object[] { Application.version }),
                    _notes ?? "",
                    new PromptChoice("@UI:txtGotIt", ButtonVariant.Accent));

                try
                {
                    await _whatsNew.MarkAsSeenAsync();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[WhatsNewPrompt] Failed to mark What's New as seen: {ex.Message}");
                }
                return HomePromptResult.Dismissed;
            }
        }
    }
}
