using System;

using Unity.AppUI.MVVM;

using eu.foodmission.platform.Components;

namespace eu.foodmission.platform
{
    /// <summary>Explanation of one progress wheel: which wheel and the user's cached progress on it.</summary>
    [ObservableObject]
    public partial class ProgressWheelInfoViewModel : ViewModelBase
    {
        [ObservableProperty]
        private string _kind = string.Empty;

        [ObservableProperty]
        private ProgressWheel _wheel;

        /// <summary>Raised when the requested wheel is unknown.</summary>
        public event Action BackRequested;

        public ProgressWheelInfoViewModel(IStoreService storeService) : base(storeService)
        {
        }

        public void Load(string kind)
        {
            if (ProgressWheelInfoCatalog.ParagraphCount(kind) == 0)
            {
                BackRequested?.Invoke();
                return;
            }

            Kind = kind;
            Wheel = FindCachedWheel(kind);
        }

        private ProgressWheel FindCachedWheel(string kind)
        {
            ProgressWheel[] wheels = _storeService?.GetAppState()?.progressWheels;
            if (wheels == null)
            {
                return null;
            }
            foreach (ProgressWheel wheel in wheels)
            {
                if (wheel != null && wheel.kind == kind)
                {
                    return wheel;
                }
            }
            return null;
        }
    }
}
