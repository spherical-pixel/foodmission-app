using Unity.AppUI.MVVM;

namespace eu.foodmission.platform
{
    public partial class GroupsJoinViewModel : ViewModelBase
    {
        public GroupsJoinViewModel(IStoreService storeService) : base(storeService) { }
    }
}
