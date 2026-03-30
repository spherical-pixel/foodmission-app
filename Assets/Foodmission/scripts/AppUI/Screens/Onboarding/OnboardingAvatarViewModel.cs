using Unity.AppUI.MVVM;

namespace eu.foodmission.platform
{
    public partial class OnboardingAvatarViewModel : ViewModelBase
    {
        public OnboardingAvatarViewModel(IStoreService storeService) : base(storeService) { }
    }
}
