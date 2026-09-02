using System.Threading.Tasks;
using Unity.AppUI.MVVM;
using UnityEngine;

namespace eu.foodmission.platform
{
    [ObservableObject]
    public partial class FoodFactScreenViewModel : ViewModelBase
    {
        [ObservableProperty]
        private FoodFact _foodFactData;

        [ObservableProperty]
        private ApiErrorResponse _errorDetail;

        [ObservableProperty]
        private bool _isLoading;

        private readonly IFoodFactService _foodFactService;

        public FoodFactScreenViewModel(
            IStoreService storeService,
            IFoodFactService foodFactService) : base(storeService)
        {
            _foodFactService = foodFactService;
        }

        public async Task LoadFoodFactDataByCodeOrId(string codeOrId)
        {
            if (string.IsNullOrEmpty(codeOrId))
                return;

            if (_foodFactService != null)
            {
                IsLoading = true;
                ErrorDetail = null;

                var (result, error) = await _foodFactService.GetFoodFactAsync(codeOrId);

                if (error != null)
                {
                    ErrorDetail = error;
                    IsLoading = false;
                    return;
                }

                ErrorDetail = null;
                IsLoading = false;
                FoodFactData = result;

                Debug.Log($"[{GetType().Name}] LoadFoodFactDataByCodeOrId -> {result?.code}");
            }
        }
    }
}
