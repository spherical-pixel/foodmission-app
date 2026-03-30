using Unity.AppUI.MVVM;

namespace eu.foodmission.platform
{
    public partial class GroupsViewModel : ViewModelBase
    {
        public GroupsViewModel(IStoreService storeService) : base(storeService) { }
    }
}
