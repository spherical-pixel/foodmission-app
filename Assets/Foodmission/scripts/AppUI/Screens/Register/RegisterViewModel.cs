using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation.Generated;
using Unity.AppUI.Redux;
using Unity.AppUI.UI;
using UnityEngine;

namespace eu.foodmission.platform
{
    [ObservableObject]
    public partial class RegisterViewModel : ViewModelBase
    {
        private readonly IAuthService _authService;

        // Datos de países cargados desde JSON
        private List<CountryData> _countriesData = new();

        private string _errorMessage = "";

        [ObservableProperty]
        private string _username = "";

        [ObservableProperty]
        private string _usernameHelpTextValue = "";

        [ObservableProperty]
        private HelpTextVariant _usernameHelpTextVariant = HelpTextVariant.Default;

        [ObservableProperty]
        private string _email = "";

        [ObservableProperty]
        private string _emailHelpTextValue = "";

        [ObservableProperty]
        private HelpTextVariant _emailHelpTextVariant = HelpTextVariant.Default;

        [ObservableProperty]
        private string _password = "";

        [ObservableProperty]
        private int _yearOfBirth = 0;

        [ObservableProperty]
        private int _selectedCountryIndex = -1;

        [ObservableProperty]
        private int _selectedRegionIndex = -1;

        [ObservableProperty]
        private string _postalCode = "";

        /// <summary>
        /// Lista de opciones de países para el dropdown (formato: "🇦🇹 Austria")
        /// </summary>
        public List<string> CountryOptions { get; private set; } = new();

        /// <summary>
        /// Lista de opciones de regiones para el país seleccionado
        /// </summary>
        public List<string> RegionOptions { get; private set; } = new();

        
        public event System.Action<string> ShowErrorRequest;

        public RegisterViewModel(IAuthService authService, IStoreService storeService) : base(storeService)
        {
            _authService = authService;

            LoadCountriesData();

            AppState state = _storeService.GetAppState();
            SynchronizeState(state);

            _storeSubscription = _store.Subscribe(
                SelectAuthState,
                OnAuthStateChanged
            );

        }

        





        /// <summary>
        /// Carga los datos de países desde el JSON en Resources
        /// </summary>
        private void LoadCountriesData()
        {
            var jsonAsset = Resources.Load<TextAsset>("ue_countries_regions");
            if (jsonAsset == null)
            {
                Debug.LogError($"[{GetType().Name}] - LoadCountriesData - ue_countries_regions.json could not be loaded");
                return;
            }

            var wrapper = JsonUtility.FromJson<CountriesList>("{\"countries\":" + jsonAsset.text + "}");
            if (wrapper?.countries == null)
            {
                Debug.LogError($"[{GetType().Name}] - LoadCountriesData - Error parsing countries JSON");
                return;
            }

            _countriesData = wrapper.countries;

            CountryOptions = _countriesData.Select(c => $"{c.country_name_local}").ToList();
        }

        /// <summary>
        /// Actualiza las regiones disponibles según el país seleccionado
        /// </summary>
        public void UpdateRegionsForSelectedCountry()
        {
            if (SelectedCountryIndex < 0 || SelectedCountryIndex >= _countriesData.Count)
            {
                RegionOptions = new List<string>();
                SelectedRegionIndex = -1;
                return;
            }

            var country = _countriesData[SelectedCountryIndex];
            RegionOptions = country.regions?
                .Select(r => r.region_name_local)
                .ToList() ?? new List<string>();

            SelectedRegionIndex = RegionOptions.Count > 0 ? 0 : -1;
        }

        /// <summary>
        /// Obtiene el código ISO del país seleccionado
        /// </summary>
        public string GetSelectedCountryIso()
        {
            if (SelectedCountryIndex < 0 || SelectedCountryIndex >= _countriesData.Count)
                return null;
            return _countriesData[SelectedCountryIndex].country_iso;
        }

        /// <summary>
        /// Obtiene el código ISO de la región seleccionada
        /// </summary>
        public string GetSelectedRegionIso()
        {
            if (SelectedCountryIndex < 0 || SelectedCountryIndex >= _countriesData.Count)
                return null;

            var country = _countriesData[SelectedCountryIndex];
            if (SelectedRegionIndex < 0 || SelectedRegionIndex >= country.regions?.Count)
                return null;

            return country.regions[SelectedRegionIndex].region_iso;
        }

        private (bool isAuthenticating, string authError, string userId) SelectAuthState(PartitionedState state)
        {
            AppState appState = state.Get<AppState>(StoreService.APP_SLICE);
            return (appState.isAuthenticating, appState.authError, appState.userId);
        }

        private void OnAuthStateChanged((bool isAuthenticating, string authError, string userId) authState)
        {
            if (!string.IsNullOrEmpty(authState.authError))
            {
                Debug.LogError($"[{GetType().Name}] - OnAuthStateChanged -> authError = {authState.authError}");
                ShowErrorRequest?.Invoke(authState.authError);
            }
            else if (!string.IsNullOrEmpty(authState.userId))
            {
                Debug.Log($"[{GetType().Name}] - OnAuthStateChanged -> userId = {authState.userId}, navigating to home");
                RaiseNavigationRequested(Actions.loading_to_home);
            }
        }

        private void SynchronizeState(AppState state)
        {
            // IsLoading = state.isAuthenticating;
            // ErrorMessage = state.authError;
            // IsAuthenticated = !string.IsNullOrEmpty(state.userId);
        }

        public async void Register()
        {
            
            
            _errorMessage = String.Empty;

            // // Validations
            // if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Password))
            // {
            //     _errorMessage = "Por favor, rellena todos los campos";
            // }
            // else if (Username.Length < 3)
            // {
            //     _errorMessage = "El nombre de usuario debe tener al menos 3 caracteres";
            // }
            // else if (!Email.Contains("@") || !Email.Contains("."))
            // {
            //     _errorMessage = "Por favor, introduce un email válido";
            // }
            // else if (Password.Length < 6)
            // {
            //     _errorMessage = "La contraseña debe tener al menos 6 caracteres";
            // }

            // if (!string.IsNullOrEmpty(_errorMessage))
            // {
            //     ShowErrorRequest?.Invoke(_errorMessage);
            //     return;
            // }

            

            // Get country and region ISO codes
            string countryIso = GetSelectedCountryIso();
            string regionIso = GetSelectedRegionIso();


            // Vamos a ver los datos que vamos a mandar antes de nada

            
            return;
            // Call RegisterAsync with optional fields
            var result = await _authService.RegisterAsync(
                Username,
                Email,
                Password,
                YearOfBirth,
                country: !string.IsNullOrEmpty(countryIso) ? countryIso : null,
                region: !string.IsNullOrEmpty(regionIso) ? regionIso : null,
                zip: !string.IsNullOrEmpty(PostalCode) ? PostalCode : null
            );

            if (result.success)
            {
                // Registration and auto-login successful - navigation handled by auth state change
                Debug.Log($"[RegisterViewModel] Registration completed successfully for user: {result.userId}");
            }
            else
            {
                ShowErrorRequest?.Invoke(result.error);
            }
        }


        

        public void ValidateUsername()
        {
            if (string.IsNullOrEmpty(Username))
            {
                UsernameHelpTextValue = "El campo no puede estar vacio";
                UsernameHelpTextVariant = HelpTextVariant.Destructive;
                return;
            }
            
            UsernameHelpTextValue = string.Empty;
            UsernameHelpTextVariant = HelpTextVariant.Default;
        }


        public void ValidateEmail()
        {
            if (string.IsNullOrEmpty(Email))
            {
                EmailHelpTextValue = "El campo no puede estar vacio";
                EmailHelpTextVariant = HelpTextVariant.Destructive;
                return;
            }

            if (!Email.Contains("@") || !Email.Contains("."))
            {
                EmailHelpTextValue = "Por favor, introduce un email válido";
                EmailHelpTextVariant = HelpTextVariant.Destructive;
                return;
            }

            EmailHelpTextValue = string.Empty;
            EmailHelpTextVariant = HelpTextVariant.Default;
        }


        

        


        

        



        
        

        
    }
}
