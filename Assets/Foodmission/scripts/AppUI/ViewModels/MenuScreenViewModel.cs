
using Unity.AppUI.MVVM;

namespace eu.foodmission.platform
{
    
    [ObservableObject]
    public partial class MenuScreenViewModel : ViewModelBase
    {
        

        public MenuScreenViewModel(IStoreService storeService) : base(storeService)
        {
            
        }
    }
}
