using eu.foodmission.platform.Components;
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

        // Period stepper
        private IconButton _btnPrevPeriod;
        private IconButton _btnNextPeriod;
        private Label _labelPeriod;
        private int _periodIndex = 0;
        private static readonly string[] k_PeriodChoices = { "Today", "Week", "Month" };

        // Scope stepper
        private IconButton _btnPrevScope;
        private IconButton _btnNextScope;
        private Label _labelScope;
        private int _scopeIndex = 0;
        private static readonly string[] k_ScopeChoices = { "Me", "Group" };

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

        private void CacheUIElements()
        {
            _healthProgress         = contentContainer.Q<LinearProgress>("health-progress");
            _sustainabilityProgress = contentContainer.Q<LinearProgress>("sustainability-progress");
            _knowledgeProgress      = contentContainer.Q<LinearProgress>("knowledge-progress");
            _caloriesCircular       = contentContainer.Q<CircularProgress>("calories-circular");
            _caloriesConsumedLabel  = contentContainer.Q<Label>("calories-consumed");
            _caloriesLeftLabel      = contentContainer.Q<Label>("calories-left");

            _btnPrevPeriod = contentContainer.Q<IconButton>("btn-prev-period");
            _btnNextPeriod = contentContainer.Q<IconButton>("btn-next-period");
            _labelPeriod   = contentContainer.Q<Label>("label-period");

            _btnPrevScope  = contentContainer.Q<IconButton>("btn-prev-scope");
            _btnNextScope  = contentContainer.Q<IconButton>("btn-next-scope");
            _labelScope    = contentContainer.Q<Label>("label-scope");
        }

        protected override void OnViewModelBound()
        {
            base.OnViewModelBound();
            RegisterEvents();
            RefreshStats();
            UpdateStepperStates();
        }

        private void RegisterEvents()
        {
            if (_btnPrevPeriod != null) _btnPrevPeriod.clicked += OnPrevPeriod;
            if (_btnNextPeriod != null) _btnNextPeriod.clicked += OnNextPeriod;
            if (_btnPrevScope  != null) _btnPrevScope.clicked  += OnPrevScope;
            if (_btnNextScope  != null) _btnNextScope.clicked  += OnNextScope;
        }

        private void UnregisterEvents()
        {
            if (_btnPrevPeriod != null) _btnPrevPeriod.clicked -= OnPrevPeriod;
            if (_btnNextPeriod != null) _btnNextPeriod.clicked -= OnNextPeriod;
            if (_btnPrevScope  != null) _btnPrevScope.clicked  -= OnPrevScope;
            if (_btnNextScope  != null) _btnNextScope.clicked  -= OnNextScope;
        }

        private void OnPrevPeriod()
        {
            if (_periodIndex > 0)
            {
                _periodIndex--;
                UpdateStepperStates();
            }
        }

        private void OnNextPeriod()
        {
            if (_periodIndex < k_PeriodChoices.Length - 1)
            {
                _periodIndex++;
                UpdateStepperStates();
            }
        }

        private void OnPrevScope()
        {
            if (_scopeIndex > 0)
            {
                _scopeIndex--;
                UpdateStepperStates();
            }
        }

        private void OnNextScope()
        {
            if (_scopeIndex < k_ScopeChoices.Length - 1)
            {
                _scopeIndex++;
                UpdateStepperStates();
            }
        }

        private void UpdateStepperStates()
        {
            if (_labelPeriod != null) _labelPeriod.text = k_PeriodChoices[_periodIndex];
            if (_labelScope  != null) _labelScope.text  = k_ScopeChoices[_scopeIndex];

            if (_btnPrevPeriod != null) _btnPrevPeriod.SetEnabled(_periodIndex > 0);
            if (_btnNextPeriod != null) _btnNextPeriod.SetEnabled(_periodIndex < k_PeriodChoices.Length - 1);
            if (_btnPrevScope  != null) _btnPrevScope.SetEnabled(_scopeIndex > 0);
            if (_btnNextScope  != null) _btnNextScope.SetEnabled(_scopeIndex < k_ScopeChoices.Length - 1);
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
            UnregisterEvents();

            _healthProgress         = null;
            _sustainabilityProgress = null;
            _knowledgeProgress      = null;
            _caloriesCircular       = null;
            _caloriesConsumedLabel  = null;
            _caloriesLeftLabel      = null;
            _btnPrevPeriod          = null;
            _btnNextPeriod          = null;
            _labelPeriod            = null;
            _btnPrevScope           = null;
            _btnNextScope           = null;
            _labelScope             = null;

            base.OnViewModelUnbinding();
        }
    }
}
