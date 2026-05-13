using System;
using eu.foodmission.platform.Components;
using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation;
using Unity.AppUI.UI;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace eu.foodmission.platform
{
    [Preserve]
    class HomeScreen : NavigationScreenBase<HomeScreenViewModel>
    {
        private LinearProgress _healthProgress;
        private LinearProgress _sustainabilityProgress;
        private LinearProgress _knowledgeProgress;
        private CircularProgress _caloriesCircular;
        private Label _caloriesConsumedLabel;
        private Label _caloriesLeftLabel;

        private FMArrowStepper _periodStepper;
        private FMArrowStepper _scopeStepper;
        
        private static readonly string[] k_PeriodChoices = { "@UI:TODAY", "@UI:WEEK", "@UI:MONTH" };
        private static readonly string[] k_ScopeChoices = { "@UI:ME", "@UI:GROUP" };

        protected override bool ApplySafeAreaBottom => false;
        protected override bool ApplySafeAreaLeft => false;
        protected override bool ApplySafeAreaRight => false;
        protected override bool ApplySafeAreaTop => false;
        protected override bool IsFixedContent => false;

        public HomeScreen()
        {
            InitializeComponent(App.current.services
                .GetRequiredService<ITemplateService>()
                .Get(TemplateAddresses.Home));
            CacheUIElements();
        }

        public override void OnEnter(NavController controller, NavDestination destination, Argument[] args)
        {
            base.OnEnter(controller, destination, args);

            var nutriService = App.current?.services?.GetService<INutriService>();
            if (nutriService != null)
            {
                nutriService.SetActive(true);
                nutriService.SetCameraActive(true);
            }
        }

        private void CacheUIElements()
        {
            _healthProgress         = contentContainer.Q<LinearProgress>("health-progress");
            _sustainabilityProgress = contentContainer.Q<LinearProgress>("sustainability-progress");
            _knowledgeProgress      = contentContainer.Q<LinearProgress>("knowledge-progress");
            _caloriesCircular       = contentContainer.Q<CircularProgress>("calories-circular");
            _caloriesConsumedLabel  = contentContainer.Q<Label>("calories-consumed");
            _caloriesLeftLabel      = contentContainer.Q<Label>("calories-left");

            _periodStepper = contentContainer.Q<FMArrowStepper>("period-stepper");
            _scopeStepper = contentContainer.Q<FMArrowStepper>("scope-stepper");
            
        }

        protected override void OnViewModelBound()
        {
            base.OnViewModelBound();
            RegisterEvents();
            RefreshStats();
            SetupSteppers();
        }

        private void RegisterEvents()
        {
            // if (_btnPrevPeriod != null) _btnPrevPeriod.clicked += OnPrevPeriod;
            // if (_btnNextPeriod != null) _btnNextPeriod.clicked += OnNextPeriod;
            // if (_btnPrevScope  != null) _btnPrevScope.clicked  += OnPrevScope;
            // if (_btnNextScope  != null) _btnNextScope.clicked  += OnNextScope;
        }

        private void UnregisterEvents()
        {
            // if (_btnPrevPeriod != null) _btnPrevPeriod.clicked -= OnPrevPeriod;
            // if (_btnNextPeriod != null) _btnNextPeriod.clicked -= OnNextPeriod;
            // if (_btnPrevScope  != null) _btnPrevScope.clicked  -= OnPrevScope;
            // if (_btnNextScope  != null) _btnNextScope.clicked  -= OnNextScope;
        }


        private void SetupSteppers()
        {
            if (_periodStepper != null)
            {
                _periodStepper.Choices = k_PeriodChoices;
                _periodStepper.SelectedIndex = _viewModel.SelectedTimePeriod switch
                {
                    TimePeriod.TODAY => 0,
                    TimePeriod.WEEK => 1,
                    TimePeriod.MONTH => 2,
                    _ => 0
                };
                _periodStepper.RegisterValueChangedCallback(OnPeriodChanged);
            }

            if (_scopeStepper != null)
            {
                _scopeStepper.Choices = k_ScopeChoices;
                _scopeStepper.SelectedIndex = _viewModel.SelectedUserScope switch
                {
                    UserScope.ME => 0,
                    UserScope.GROUP => 1,
                    _ => 0
                };
                _scopeStepper.RegisterValueChangedCallback(OnScopeChanged);
            }
        }

        private void OnPeriodChanged(object sender, ChangeEvent<int> evt)
        {
            TimePeriod selectedPeriod = evt.newValue switch
            {
                0 => TimePeriod.TODAY,
                1 => TimePeriod.WEEK,
                2 => TimePeriod.MONTH,
                _ => TimePeriod.TODAY
            };
            _viewModel.SetTimePeriod(selectedPeriod);
        }

        private void OnScopeChanged(object sender, ChangeEvent<int> evt)
        {
            UserScope selectedScope = evt.newValue switch
            {
                0 => UserScope.ME,
                1 => UserScope.GROUP,
                _ => UserScope.ME
            };
            _viewModel.SetUserScope(selectedScope);
        }

        

        private void RefreshStats()
        {
            if (_viewModel == null) return;

            if (_healthProgress != null)         _healthProgress.value         = _viewModel.HealthProgress;
            if (_sustainabilityProgress != null)  _sustainabilityProgress.value = _viewModel.SustainabilityProgress;
            if (_knowledgeProgress != null)       _knowledgeProgress.value      = _viewModel.KnowledgeProgress;

            int total = _viewModel.CaloriesConsumed + _viewModel.CaloriesLeft;
            if (_caloriesCircular != null)
            {
                _caloriesCircular.value = total > 0 ? (float)_viewModel.CaloriesConsumed / total : 0f;
            }

            if (_caloriesConsumedLabel != null) _caloriesConsumedLabel.text = _viewModel.CaloriesConsumed.ToString();
            if (_caloriesLeftLabel != null)     _caloriesLeftLabel.text     = _viewModel.CaloriesLeft.ToString();
        }

        protected override void OnViewModelUnbinding()
        {
            var nutriService = App.current?.services?.GetService<INutriService>();
            if (nutriService != null)
            {
                nutriService.SetCameraActive(false);
                nutriService.SetActive(false);
            }

            UnregisterEvents();

            _healthProgress         = null;
            _sustainabilityProgress = null;
            _knowledgeProgress      = null;
            _caloriesCircular       = null;
            _caloriesConsumedLabel  = null;
            _caloriesLeftLabel      = null;
            _periodStepper = null;
            _scopeStepper = null;
            

            base.OnViewModelUnbinding();
        }
    }
}
