using Unity.AppUI.MVVM;

namespace eu.foodmission.platform
{
    public partial class OnboardingGroupsViewModel : ViewModelBase
    {
        public OnboardingGroupsViewModel(IStoreService storeService) : base(storeService) { }
    }
}
