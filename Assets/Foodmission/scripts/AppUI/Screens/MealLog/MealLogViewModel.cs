using Unity.AppUI.MVVM;

namespace eu.foodmission.platform
{
    public partial class MealLogViewModel : ViewModelBase
    {
        public MealLogViewModel(IStoreService storeService) : base(storeService) { }
    }
}
