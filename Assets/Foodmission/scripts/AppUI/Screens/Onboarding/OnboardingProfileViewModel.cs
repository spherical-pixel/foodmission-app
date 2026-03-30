using Unity.AppUI.MVVM;

namespace eu.foodmission.platform
{
    public partial class OnboardingProfileViewModel : ViewModelBase
    {
        public OnboardingProfileViewModel(IStoreService storeService) : base(storeService) { }
    }
}
