using Unity.AppUI.MVVM;

namespace eu.foodmission.platform
{
    public partial class ShoppingListDetailViewModel : ViewModelBase
    {
        public ShoppingListDetailViewModel(IStoreService storeService) : base(storeService) { }
    }
}
