using Unity.AppUI.MVVM;

namespace eu.foodmission.platform
{
    public partial class MealLogAddViewModel : ViewModelBase
    {
        public MealLogAddViewModel(IStoreService storeService) : base(storeService) { }
    }
}
