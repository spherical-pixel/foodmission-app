using Unity.AppUI.MVVM;

namespace eu.foodmission.platform
{
    public partial class GroupDetailViewModel : ViewModelBase
    {
        public GroupDetailViewModel(IStoreService storeService) : base(storeService) { }
    }
}
