using Unity.AppUI.MVVM;

namespace eu.foodmission.platform
{
    public partial class PantryItemDetailViewModel : ViewModelBase
    {
        public PantryItemDetailViewModel(IStoreService storeService) : base(storeService) { }
    }
}
