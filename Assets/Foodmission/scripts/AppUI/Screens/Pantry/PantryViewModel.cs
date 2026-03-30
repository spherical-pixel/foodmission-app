using Unity.AppUI.MVVM;

namespace eu.foodmission.platform
{
    public partial class PantryViewModel : ViewModelBase
    {
        public PantryViewModel(IStoreService storeService) : base(storeService) { }
    }
}
