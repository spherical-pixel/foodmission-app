using Unity.AppUI.MVVM;

namespace eu.foodmission.platform
{
    public partial class GroupsCreateViewModel : ViewModelBase
    {
        public GroupsCreateViewModel(IStoreService storeService) : base(storeService) { }
    }
}
