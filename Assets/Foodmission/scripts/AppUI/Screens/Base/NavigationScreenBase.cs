using System;
using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace eu.foodmission.platform
{
    /// <summary>
    /// Base class for all Navigation Screens in the application.
    /// Manages lifecycle of the ViewModel, binding and navigation.
    /// </summary>
    /// <typeparam name="TViewModel">The ViewModel associated to this screen</typeparam>
    [Preserve]
    public abstract class NavigationScreenBase<TViewModel> : NavigationScreen where TViewModel : class, IDisposable, INavigationAware
    {
        // --------------------------------------------------------------------
        // Dependencies
        // --------------------------------------------------------------------

        protected TViewModel _viewModel;
        protected IThemeService _themeService;
        protected NavController _navController;

        // --------------------------------------------------------------------
        // Construction & UI Setup
        // --------------------------------------------------------------------

        /// <summary>        
        /// Flag to indicate if the content is fixed (no scrollable)
        /// Override in the derived class to change the behavior
        /// </summary>
        protected virtual bool IsFixedContent => true;

        /// <summary>
        /// Flag to indicate if the safe area padding top should be applied
        /// Override in the derived class to change the behavior
        /// </summary>
        protected virtual bool ApplySafeAreaTop => true;

        /// <summary>
        /// Flag to indicate if the safe area padding bottom should be applied
        /// Override in the derived class to change the behavior
        /// </summary>
        protected virtual bool ApplySafeAreaBottom => true;

        /// <summary>
        /// Flag to indicate if the safe area padding left should be applied
        /// Override in the derived class to change the behavior
        /// </summary>
        protected virtual bool ApplySafeAreaLeft => true;

        /// <summary>
        /// Flag to indicate if the safe area padding right should be applied
        /// Override in the derived class to change the behavior
        /// </summary>
        protected virtual bool ApplySafeAreaRight => true;

        /// <summary>
        /// Initializes the visual component with the UXML template
        /// Llama a esto desde el constructor de la clase derivada
        /// Call this from the constructor of the derived class
        /// </summary>
        protected void InitializeComponent(VisualTreeAsset template)
        {
            ConfigureScrollView();
            ApplyTemplate(template);
        }

        /// <summary>
        /// Setup the scroll view according to IsFixedContent
        /// </summary>
        private void ConfigureScrollView()
        {
            if (IsFixedContent)
            {
                AddToClassList("fm-screen-fixed");
            }

            // By now scrollbars are going to be hidden always
            scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            scrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden;
        }

        /// <summary>
        /// Clones the UXML template into the contentContainer
        /// </summary>
        private void ApplyTemplate(VisualTreeAsset template)
        {
            if (template != null)
            {
                template.CloneTree(contentContainer);
                // Grow the container to fill the available space
                contentContainer.style.flexGrow = 1;
            }
            else
            {
                Debug.LogError($"[{GetType().Name}] - ApplyTemplate -> Template is null");
            }
        }

        // --------------------------------------------------------------------
        // Lifecycle - Screen Entry
        // --------------------------------------------------------------------
        
        /// <summary>
        /// Called when the screen is entered. This method is called before the screen is displayed.
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="destination"></param>
        /// <param name="args"></param>
        public override void OnEnter(NavController controller, NavDestination destination, Argument[] args)
        {
            base.OnEnter(controller, destination, args);
            _navController = controller;

            ResolveDependencies();
            BindViewModel();
            SubscribeToNavigation();
            OnViewModelBound();
        }

        /// <summary>
        /// Resolves dependencies
        /// </summary>
        private void ResolveDependencies()
        {
            _viewModel = App.current.services.GetRequiredService<TViewModel>();
            _themeService = App.current.services.GetRequiredService<IThemeService>();
        }

        /// <summary>
        /// Establece el dataSource para binding declarativo y aplica safe area.
        /// Establiblese dataSource for declarative binding and applies safeArea if needed.
        /// </summary>
        private void BindViewModel()
        {
            contentContainer.dataSource = _viewModel;
            _themeService?.ApplySafeAreaPadding(contentContainer,ApplySafeAreaTop,ApplySafeAreaBottom,ApplySafeAreaLeft,ApplySafeAreaRight);
        }

        /// <summary>        
        /// Subscribes to the ViewModel's navigation event.
        /// </summary>
        private void SubscribeToNavigation()
        {
            _viewModel.NavigationRequested += OnNavigationRequested;
        }

        /// <summary>
        /// Virtual method for additional logic after the ViewModel is bound.
        /// </summary>
        protected virtual void OnViewModelBound() { }

        // --------------------------------------------------------------------
        // Lifecycle - Screen Exit
        // --------------------------------------------------------------------

        /// <summary>
        /// Called when navigation exits this screen.
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="destination"></param>
        /// <param name="args"></param>
        public override void OnExit(NavController controller, NavDestination destination, Argument[] args)
        {
            OnViewModelUnbinding();

            UnsubscribeFromNavigation();
            DisposeViewModel();

            base.OnExit(controller, destination, args);
        }

        /// <summary>
        /// Unsubscribes from navigation event
        /// </summary>
        private void UnsubscribeFromNavigation()
        {
            if (_viewModel != null)
            {
                _viewModel.NavigationRequested -= OnNavigationRequested;
            }
        }


        /// <summary>
        /// Dispose ViewModel and clean references
        /// </summary>
        private void DisposeViewModel()
        {
            _viewModel?.Dispose();
            _viewModel = null;
            _themeService = null;
            _navController = null;
        }

        /// <summary>
        /// Virtual methos for additional logic before unbinding the ViewModel.
        /// </summary>
        protected virtual void OnViewModelUnbinding() { }

        // --------------------------------------------------------------------
        // Navigation Handling
        // --------------------------------------------------------------------

        /// <summary>
        /// Callback when ViewModel asks for navigation.
        /// </summary>
        protected virtual void OnNavigationRequested(string navigationAction)
        {
            if (_navController != null)
            {
                _navController.Navigate(navigationAction);
            }
            else
            {
                Debug.LogError($"[{GetType().Name}] - OnNavigationRequested - Cannot navigate - NavController is null");
            }
        }
    }
}
