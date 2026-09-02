using System;
using System.Collections.Generic;
using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation;
using Unity.AppUI.Navigation.Generated;
using UnityEngine;

namespace eu.foodmission.platform
{
    public class KnowledgeSectionItem
    {
        public string Id { get; set; }
        public string TitleKey { get; set; }
        public string Title { get; set; }
        public string DescriptionKey { get; set; }
        public string BannerAddress { get; set; }
        public string NavigationAction { get; set; }
        public bool IsEnabled { get; set; } = true;
    }

    [ObservableObject]
    public partial class KnowledgeViewModel : ViewModelBase
    {
        [ObservableProperty]
        private IReadOnlyList<KnowledgeSectionItem> _sections = new List<KnowledgeSectionItem>();


        public KnowledgeViewModel(IStoreService storeService) : base(storeService)
        {
            InitializeSections();
        }

        private void InitializeSections()
        {
            var list = new List<KnowledgeSectionItem>
            {
                new KnowledgeSectionItem
                {
                    Id = "quizzes",
                    TitleKey = "NAV_QUIZZES",
                    Title = "Quizzes",
                    BannerAddress = "knowledge/quiz",
                    NavigationAction = Actions.go_to_quizzes,
                    IsEnabled = true
                },
                new KnowledgeSectionItem
                {
                    Id = "food_facts",
                    TitleKey = "NAV_FOOD_FACTS",
                    Title = "Food Facts",
                    BannerAddress = "knowledge/foodfacts",
                    NavigationAction = Actions.go_to_food_facts,
                    IsEnabled = true
                }
            };

            Sections = list;
        }

        public void OpenSection(KnowledgeSectionItem section)
        {
            if (section == null || !section.IsEnabled || string.IsNullOrEmpty(section.NavigationAction))
                return;

            RaiseNavigationRequested(section.NavigationAction);
        }

        public void OpenQuizzes()
        {
            RaiseNavigationRequested(Actions.go_to_quizzes);
        }

        public void OpenFoodFacts()
        {
            RaiseNavigationRequested(Actions.go_to_food_facts);
        }
    }
}
