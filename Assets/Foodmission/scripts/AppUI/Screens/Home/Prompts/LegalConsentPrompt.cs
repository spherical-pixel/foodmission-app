using System.Collections.Generic;
using System.Threading.Tasks;

using eu.foodmission.platform.Components;

using Unity.AppUI.MVVM;
using Unity.AppUI.UI;

using UnityEngine;
using UnityEngine.Localization.Settings;

namespace eu.foodmission.platform
{
    /// <summary>A new legal document must be accepted before anything else; declining offers log out or delete account.</summary>
    public sealed class LegalConsentPrompt : IHomePrompt
    {
        private readonly HomeScreenViewModel _viewModel;

        public LegalConsentPrompt(HomeScreenViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        public HomePromptKind Kind => HomePromptKind.Blocking;

        /// <summary>Every document the user still has to accept, in the backend's order.</summary>
        public static IReadOnlyList<PendingLegalConsent> PendingDocuments(LegalConsentStatus status)
        {
            var pending = new List<PendingLegalConsent>();
            if (status == null || !status.mustAccept || status.documents == null)
            {
                return pending;
            }

            foreach (PendingLegalConsent doc in status.documents)
            {
                if (doc != null && !doc.accepted)
                {
                    pending.Add(doc);
                }
            }
            return pending;
        }

        public async Task<IHomePromptInstance> CheckAsync()
        {
            IReadOnlyList<PendingLegalConsent> pending = PendingDocuments(await _viewModel.CheckPendingLegalConsentAsync());
            return pending.Count == 0 ? null : new Instance(_viewModel, pending);
        }

        private sealed class Instance : IHomePromptInstance
        {
            private const int Review = 0;
            private const int LogOut = 1;
            private const int Delete = 2;

            private readonly HomeScreenViewModel _viewModel;
            private readonly IReadOnlyList<PendingLegalConsent> _pending;

            public Instance(HomeScreenViewModel viewModel, IReadOnlyList<PendingLegalConsent> pending)
            {
                _viewModel = viewModel;
                _pending = pending;
            }

            public async Task<HomePromptResult> ShowAsync(IHomePromptHost host)
            {
                // Every pending document blocks: nothing else is shown until all of them are accepted
                foreach (PendingLegalConsent doc in _pending)
                {
                    if (await ShowOneAsync(host, doc) == HomePromptResult.Navigated)
                    {
                        return HomePromptResult.Navigated;
                    }
                }
                return HomePromptResult.Dismissed;
            }

            private async Task<HomePromptResult> ShowOneAsync(IHomePromptHost host, PendingLegalConsent pending)
            {
                LegalDocument document = await _viewModel.GetLegalDocumentAsync(pending.docType);
                string title = !string.IsNullOrEmpty(document?.title)
                    ? document.title
                    : (pending.docType == LegalDocType.TermsOfService ? "@UI:T&C_TITLE" : "@UI:PRIVACY_POLICY_TITLE");
                string content = document?.content ?? "";

                int view = await HomePromptDialogs.ChooseNutriAsync(
                    LocalizationSettings.StringDatabase.GetLocalizedString("UI", "NEW_LEGAL_DOC", new object[] { title }),
                    new PromptChoice("@UI:MENU_VIEW", ButtonVariant.Accent));
                if (view == HomePromptDialogs.Closed || !host.IsActive)
                {
                    // Not accepted yet: nothing else is shown until it is (asked again on the next Home entry)
                    return HomePromptResult.Navigated;
                }

                while (host.IsActive)
                {
                    if (await HomePromptDialogs.ShowDocumentAsync(host.DialogAnchor, title, content))
                    {
                        await _viewModel.AcceptLegalConsentAsync(pending.documentKey);
                        return HomePromptResult.Dismissed;
                    }

                    int choice = await HomePromptDialogs.ChooseInfoAsync(host.DialogAnchor, "@UI:MESSAGE_TITLE_WARNING",
                        LocalizationSettings.StringDatabase.GetLocalizedString("UI", "NOT_ACCP_LEGAL_WARNING", new object[] { title }),
                        new PromptChoice("@UI:TXT_REVIEW_DOCUMENT", ButtonVariant.Accent),
                        new PromptChoice("@UI:LOG_OUT", ButtonVariant.Accent),
                        new PromptChoice("@UI:DELETE_ACCOUNT", ButtonVariant.Destructive));
                    if (choice == LogOut)
                    {
                        App.current?.services?.GetService<IAuthService>()?.Logout();
                        host.NavigateToAuth();
                        return HomePromptResult.Navigated;
                    }
                    if (choice == Delete && await DeleteAccountAsync(host))
                    {
                        return HomePromptResult.Navigated;
                    }
                    // Review, closed, or delete cancelled/failed: show the document again
                }
                return HomePromptResult.Navigated;
            }

            private static Task<bool> DeleteAccountAsync(IHomePromptHost host)
            {
                var result = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                bool pressed = false;
                FMDialog.ShowAlert(
                    App.current?.rootVisualElement,
                    "@UI:DELETE_ACCOUNT_TITLE",
                    "@UI:DELETE_ACCOUNT_MESSAGE",
                    AlertSemantic.Destructive,
                    "@UI:TXT_ACCEPT", onOk: async () =>
                    {
                        pressed = true;
                        var authService = App.current?.services?.GetService<IAuthService>();
                        if (authService == null)
                        {
                            result.TrySetResult(false);
                            return;
                        }

                        var (success, error) = await authService.DeleteAccountAsync();
                        if (success)
                        {
                            authService.Logout();
                            host.NavigateToAuth();
                            result.TrySetResult(true);
                        }
                        else
                        {
                            Debug.LogError($"[LegalConsentPrompt] Delete account failed: {error}");
                            result.TrySetResult(false);
                        }
                    },
                    "@UI:TXT_CANCEL", onKo: () =>
                    {
                        pressed = true;
                        result.TrySetResult(false);
                    },
                    onDismissed: () =>
                    {
                        if (!pressed)
                        {
                            result.TrySetResult(false);
                        }
                    });
                return result.Task;
            }
        }
    }
}
