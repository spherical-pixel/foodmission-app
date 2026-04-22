using System;
using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation.Generated;
using Unity.AppUI.Redux;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.UIElements;


namespace eu.foodmission.platform
{
    [ObservableObject]
    public partial class CompleteWelcomeViewModel : ViewModelBase
    {
        
        
        // /// <summary>
        // /// Username for auth
        // /// </summary>
        // [ObservableProperty]
        // private string _username = "";

        
        

        public CompleteWelcomeViewModel(IAuthService authService, IStoreService storeService) : base(storeService)
        {
            
        }


    }

        

}
