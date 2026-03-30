using Unity.AppUI.MVVM;

namespace eu.foodmission.platform
{
    public partial class NotificationsViewModel : ViewModelBase
    {
        public NotificationsViewModel(IStoreService storeService) : base(storeService) { }
    }
}
