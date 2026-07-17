using System;
using System.Threading.Tasks;
using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation;
using UnityEngine;

namespace eu.foodmission.platform
{
    [ObservableObject]
    public partial class TestSurveyViewModel : StepFlowViewModelBase
    {
        // Step 1 State
        [ObservableProperty] private bool m_IsVegan;
        [ObservableProperty] private bool m_IsVegetarian;
        [ObservableProperty] private bool m_IsOmnivore;

        // Step 2 State
        [ObservableProperty] private int m_RatingValue = 0;

        // Step 3 State
        [ObservableProperty] private string m_FeedbackText = "";

        public TestSurveyViewModel(IStoreService storeService) : base(storeService)
        {
        }

        protected override int GetStepCount() => 3;

        protected override string GetStepTitle(int stepIndex)
        {
            return stepIndex switch
            {
                0 => "Diet Preference",
                1 => "Rating",
                2 => "Feedback",
                _ => ""
            };
        }

        protected override bool ValidateStep(int stepIndex)
        {
            return stepIndex switch
            {
                0 => IsVegan || IsVegetarian || IsOmnivore,
                1 => RatingValue > 0,
                2 => !string.IsNullOrWhiteSpace(FeedbackText),
                _ => false
            };
        }

        protected override Task OnStepEnteredAsync(int stepIndex)
        {
            Debug.Log($"[TestSurvey] Entered Step {stepIndex}");
            return Task.CompletedTask;
        }

        protected override Task OnStepExitingAsync(int stepIndex)
        {
            Debug.Log($"[TestSurvey] Exiting Step {stepIndex}");
            return Task.CompletedTask;
        }

        protected override Task OnFlowCompletedAsync()
        {
            Debug.Log($"[TestSurvey] Survey Completed! Diet: Vegan={IsVegan}, Veg={IsVegetarian}, Omni={IsOmnivore}, Rating={RatingValue}, Feedback={FeedbackText}");
            // Navigate back home or pop back stack
            CancelFlow();
            return Task.CompletedTask;
        }
    }
}
