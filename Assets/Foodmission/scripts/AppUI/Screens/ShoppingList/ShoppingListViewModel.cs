using Unity.AppUI.MVVM;

namespace eu.foodmission.platform
{
    public partial class ShoppingListViewModel : ViewModelBase
    {
        public ShoppingListViewModel(IStoreService storeService) : base(storeService) { }
    }
}
