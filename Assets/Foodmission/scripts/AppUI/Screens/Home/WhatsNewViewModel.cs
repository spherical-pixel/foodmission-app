using Unity.AppUI.MVVM;
using UnityEngine;

namespace eu.foodmission.platform
{
    [ObservableObject]
    public partial class WhatsNewViewModel : ViewModelBase
    {
        [ObservableProperty] private string _version;
        [ObservableProperty] private string _releaseNotes;
        [ObservableProperty] private bool _isVisible;

        private readonly IWhatsNewService _whatsNewService;

        public WhatsNewViewModel(IStoreService storeService, IWhatsNewService whatsNewService)
            : base(storeService)
        {
            _whatsNewService = whatsNewService;
        }

        public async void CheckAndShowAsync()
        {
            var (shouldShow, notes) = await _whatsNewService.CheckShouldShowAsync();
            if (shouldShow)
            {
                Version = Application.version;
                ReleaseNotes = notes ?? "";
                IsVisible = true;
            }
        }

        public async void Dismiss()
        {
            IsVisible = false;
            await _whatsNewService.MarkAsSeenAsync();
        }
    }
}
