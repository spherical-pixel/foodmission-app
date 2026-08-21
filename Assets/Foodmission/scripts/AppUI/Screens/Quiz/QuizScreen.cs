
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using eu.foodmission.platform.Components;
using MainraGames;
using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation;
using Unity.AppUI.Navigation.Generated;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.Localization.Settings;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace eu.foodmission.platform
{
    [Preserve]
    class QuizScreen : NavigationScreenBase<QuizScreenViewModel>
    {
        override protected bool IsFixedContent => true;
        override protected bool ApplySafeAreaTop => true;
        override protected bool ApplySafeAreaBottom => false;
        override protected bool ApplySafeAreaLeft => false;
        override protected bool ApplySafeAreaRight => false;


        private FMResponseQuiz _response1;
        private FMResponseQuiz _response2;
        private FMResponseQuiz _response3;
        private FMResponseQuiz _response4;
        private FMResponseQuiz _currentResponse = null;
        private FMButton _btContinue;

        private Text _questionText;
        private VisualElement _mainQuestionCard;
        private VisualElement _explanationCard;
        private Text _explanationText;
        private FMSourceItemView _sourceItemView;
        private Image _imageTopic;

        private VisualElement _icnOk;
        private VisualElement _icnKo;
        private UIParticle _particles;


        private IAudioService _audioService;
        private IAvatarService _avatarService;
        private IDimensionService _dimensionService;

        public QuizScreen()
        {
            InitializeComponent(App.current.services
                .GetRequiredService<ITemplateService>()
                .Get(TemplateAddresses.QuizScreen));
            _audioService = App.current?.services?.GetService<IAudioService>();
            _avatarService = App.current?.services?.GetService<IAvatarService>();
            _dimensionService = App.current?.services?.GetService<IDimensionService>();
            _avatarService.AvatarController.AvatarAnimationController.CurrentMood = AvatarMood.Neutral;
            CacheUIElements();
            RegisterManualEvents();
            DisableQuizButtons();


        }

        public override void OnEnter(NavController controller, NavDestination destination, Argument[] args)
        {
            base.OnEnter(controller, destination, args);

            if (args != null)
            {
                // _navController.Navigate(Actions.open_quiz, new[] { new Argument("code", "Q1.1.1"), new Argument("id", "df27b23d-ea27-4c7e-93f5-26e3307fefdf") });
                foreach (var a in args)
                {
                    if (a.name == "code" || a.name == "id")
                    {
                        RequestLoadQuiz(a.value);
                        break;
                    }
                }
            }

            _viewModel.PropertyChanged += OnPropertyChanged;
            HideFeedbackIcons();
            if (_particles != null)
            {
                _particles.style.display = DisplayStyle.None;
                _particles.Clear();
            }
            _audioService.PlayMusic(MusicType.Quiz);
        }

        public override void OnExit(NavController controller, NavDestination destination, Argument[] args)
        {
            _viewModel.PropertyChanged -= OnPropertyChanged;

            HideFeedbackIcons();
            if (_particles != null)
            {
                _particles.style.display = DisplayStyle.None;
                _particles.Clear();
            }
            _avatarService.AvatarController.AvatarAnimationController.CurrentMood = AvatarMood.Neutral;
            _audioService.StopMusic();

            base.OnExit(controller, destination, args);
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_viewModel.QuizData))
            {
                RebuildResponses();
                RebuildExplanationCard();
            }
            else if (e.PropertyName == nameof(_viewModel.ErrorDetail))
            {
                UpdateApiErrorState();
            }
            else if (e.PropertyName == nameof(_viewModel.QuizProgress))
            {
                // Check response
                OnProgressReceived(_viewModel.QuizProgress);
            }

        }

        private void RebuildResponses()
        {
            if (_viewModel == null || _viewModel.QuizData == null || _viewModel.QuizData.options == null)
            {
                return;
            }

            _questionText.text = _viewModel.QuizData.question;

            _response1.style.display = DisplayStyle.None;
            _response2.style.display = DisplayStyle.None;
            _response3.style.display = DisplayStyle.None;
            _response4.style.display = DisplayStyle.None;

            _response1.ClearContent();
            _response2.ClearContent();
            _response3.ClearContent();
            _response4.ClearContent();

            List<QuizOption> options = new List<QuizOption>(_viewModel.QuizData.options).OrderBy(_ => Guid.NewGuid()).ToList();
            for (int i = 0; i < options.Count; ++i)
            {
                FMResponseQuiz responseQuiz = null;
                switch (i)
                {
                    case 0:
                        responseQuiz = _response1;
                        break;
                    case 1:
                        responseQuiz = _response2;
                        break;
                    case 2:
                        responseQuiz = _response3;
                        break;
                    case 3:
                        responseQuiz = _response4;
                        break;
                }


                if (responseQuiz != null)
                {
                    responseQuiz.QuizOption = options[i];
                    responseQuiz.style.display = DisplayStyle.Flex;
                }
            }

            ShowCard(1000, EnableQuizButtons);





        }

        private void ShowCard(long delay, Action onComplete = null)
        {
            _mainQuestionCard.schedule.Execute(() =>
            {
                _mainQuestionCard.RemoveFromClassList("fm-quiz-main-card--hidden");
            }).StartingIn(delay);

            if (onComplete != null)
            {
                _mainQuestionCard.schedule.Execute(onComplete).StartingIn(400 + delay);
            }
        }

        private void HideCard(Action onComplete = null)
        {
            _mainQuestionCard.AddToClassList("fm-quiz-main-card--hidden");

            if (onComplete != null)
            {
                _mainQuestionCard.schedule.Execute(onComplete).StartingIn(400);
            }
        }

        private void ShowExplanationCard(long delay, Action onComplete = null)
        {
            _explanationCard.schedule.Execute(() =>
            {
                _explanationCard.RemoveFromClassList("fm-quiz-explanation-card--hidden");
            }).StartingIn(delay);

            if (onComplete != null)
            {
                _explanationCard.schedule.Execute(onComplete).StartingIn(400 + delay);
            }
        }

        private void HideExplanationCard(Action onComplete = null)
        {
            _explanationCard.AddToClassList("fm-quiz-explanation-card--hidden");

            if (onComplete != null)
            {
                _explanationCard.schedule.Execute(onComplete).StartingIn(400);
            }
        }

        private async Task RequestLoadQuiz(string codeOrId)
        {
            await _viewModel.LoadQuizDataByCodeOrId(codeOrId);
            Debug.Log("RequestLoadQuiz ENDED");
        }

        private void CacheUIElements()
        {
            // _btExit = contentContainer.Q<Unity.AppUI.UI.Button>("btExit");
            _response1 = contentContainer.Q<FMResponseQuiz>("response-1");
            _response2 = contentContainer.Q<FMResponseQuiz>("response-2");
            _response3 = contentContainer.Q<FMResponseQuiz>("response-3");
            _response4 = contentContainer.Q<FMResponseQuiz>("response-4");

            _btContinue = contentContainer.Q<FMButton>("bt-continue");

            _questionText = contentContainer.Q<Text>("text-question");
            _mainQuestionCard = contentContainer.Q<VisualElement>("main-question-card");
            _mainQuestionCard.AddToClassList("fm-quiz-main-card--hidden");

            _explanationCard = contentContainer.Q<VisualElement>("explanation-card");
            _explanationCard.AddToClassList("fm-quiz-explanation-card--hidden");

            _response1.style.display = DisplayStyle.None;
            _response2.style.display = DisplayStyle.None;
            _response3.style.display = DisplayStyle.None;
            _response4.style.display = DisplayStyle.None;

            _imageTopic = contentContainer.Q<Image>("image-topic");

            _explanationText = contentContainer.Q<Text>("text-explanation");
            _sourceItemView = contentContainer.Q<FMSourceItemView>("source-item-view");
            _sourceItemView.ShowPrefix = false;


            _icnOk = contentContainer.Q<VisualElement>("icn-ok");
            _icnKo = contentContainer.Q<VisualElement>("icn-ko");

            _particles = contentContainer.Q<UIParticle>("particles");
            if (_particles != null)
            {
                _particles.style.display = DisplayStyle.None;
            }


        }

        private void RegisterManualEvents()
        {
            // if (_btExit != null) _btExit.clicked += OnExitClicked;
            if (_response1 != null)
            {
                _response1.OnOptionSelected += OnOptionSelected;
            }

            if (_response2 != null)
            {
                _response2.OnOptionSelected += OnOptionSelected;
            }

            if (_response3 != null)
            {
                _response3.OnOptionSelected += OnOptionSelected;
            }

            if (_response4 != null)
            {
                _response4.OnOptionSelected += OnOptionSelected;
            }

            if (_btContinue != null)
            {
                _btContinue.clicked += OnContinueClicked;
            }
        }

        private void OnContinueClicked()
        {
            HideExplanationCard(() =>
            {
                if (_navController != null)
                {
                    _navController.PopBackStack();
                }
                else
                {
                    OnNavigationRequested(Actions.go_to_home, null);
                }
            });
        }

        private void OnOptionSelected(FMResponseQuiz response)
        {
            DisableQuizButtons();
            _currentResponse = response;
            Debug.Log("OnOptionSelected -> " + _currentResponse.QuizOption.label);

            _audioService.PlaySfx(SfxType.PositiveButton);
            _currentResponse.SetState(QuizResponseState.Selected);

            _viewModel.SubmitResponse(_currentResponse.QuizOption);
        }

        private void OnProgressReceived(QuizProgress progress)
        {
            if (progress == null) return;
            if (progress.isCorrect != null)
            {

                schedule.Execute(() =>
                {
                    if (progress.isCorrect.Value == true)
                    {
                        SetCorrect();
                    }
                    else
                    {
                        SetIncorrect();
                    }
                }).StartingIn(1500);


            }
        }

        private void SetCorrect()
        {
            _currentResponse.SetState(QuizResponseState.Correct);
            _audioService.PlaySfx(SfxType.QuizPositive);
            _avatarService.AvatarController.AvatarAnimationController.CurrentMood = AvatarMood.Happy;
            _icnOk?.RemoveFromClassList("fm-quiz-feedback-icon--hidden");
            _icnKo?.AddToClassList("fm-quiz-feedback-icon--hidden");
            if (_particles != null)
            {
                _particles.style.display = DisplayStyle.Flex;
            }

            schedule.Execute(() =>
            {
                HideCard(() =>
                {
                    _avatarService.AvatarController.AvatarAnimationController.TriggerCelebration();
                    ShowExplanationCard(1500);
                });
            }).StartingIn(1000);
        }

        private void SetIncorrect()
        {
            _currentResponse.SetState(QuizResponseState.Incorrect);
            _audioService.PlaySfx(SfxType.QuizNegative);
            _avatarService.AvatarController.AvatarAnimationController.CurrentMood = AvatarMood.Sad;
            _icnKo?.RemoveFromClassList("fm-quiz-feedback-icon--hidden");
            _icnOk?.AddToClassList("fm-quiz-feedback-icon--hidden");
            schedule.Execute(() =>
            {
                HideCard(() =>
                {
                    ShowExplanationCard(1500);
                });
            }).StartingIn(1000);
        }

        private void HideFeedbackIcons()
        {
            _icnOk?.AddToClassList("fm-quiz-feedback-icon--hidden");
            _icnKo?.AddToClassList("fm-quiz-feedback-icon--hidden");
        }

        private void RebuildExplanationCard()
        {
            if (_viewModel == null || _viewModel.QuizData == null)
            {
                return;
            }

            if (_explanationText != null)
            {
                _explanationText.text = _viewModel.QuizData.explanation ?? "";
            }

            if (_sourceItemView != null)
            {
                _sourceItemView.SetSource(_viewModel.QuizData.source);
            }

            if (_imageTopic != null)
            {
                Sprite topicSprite = _dimensionService?.GetTopicSprite(_viewModel.QuizData.topicId);
                if (topicSprite != null)
                {
                    _imageTopic.sprite = topicSprite;
                    _imageTopic.scaleMode = ScaleMode.ScaleToFit;
                    _imageTopic.style.width = Length.Percent(100);
                    _imageTopic.style.height = StyleKeyword.Auto;
                    if (topicSprite.rect.height > 0)
                    {
                        _imageTopic.style.aspectRatio = topicSprite.rect.width / topicSprite.rect.height;
                    }
                    _imageTopic.style.display = DisplayStyle.Flex;
                }
                else
                {
                    _imageTopic.sprite = null;
                    _imageTopic.style.display = DisplayStyle.None;
                }
            }
        }

        private void UnregisterManualEvents()
        {
            // if (_btExit != null) _btExit.clicked -= OnExitClicked;
            if (_response1 != null)
            {
                _response1.OnOptionSelected -= OnOptionSelected;
            }

            if (_response2 != null)
            {
                _response2.OnOptionSelected -= OnOptionSelected;
            }

            if (_response3 != null)
            {
                _response3.OnOptionSelected -= OnOptionSelected;
            }

            if (_response4 != null)
            {
                _response4.OnOptionSelected -= OnOptionSelected;
            }

            if (_btContinue != null)
            {
                _btContinue.clicked -= OnContinueClicked;
            }
        }

        private void EnableQuizButtons()
        {
            if (_response1 != null)
            {
                _response1.Clickable = true;
            }

            if (_response2 != null)
            {
                _response2.Clickable = true;
            }

            if (_response3 != null)
            {
                _response3.Clickable = true;
            }

            if (_response4 != null)
            {
                _response4.Clickable = true;
            }
        }

        private void DisableQuizButtons()
        {
            if (_response1 != null)
            {
                _response1.Clickable = false;
            }

            if (_response2 != null)
            {
                _response2.Clickable = false;
            }

            if (_response3 != null)
            {
                _response3.Clickable = false;
            }

            if (_response4 != null)
            {
                _response4.Clickable = false;
            }
        }



        // private void OnExitClicked()
        // {
        //     _viewModel?.AvatarService.LoadSavedConfig();
        //     CloseSelectorItemAvatar();
        //     OnNavigationRequested(Actions.go_to_home, null);
        // }

        protected override void OnViewModelBound()
        {
            base.OnViewModelBound();
        }

        protected override async void OnViewModelUnbinding()
        {

            UnregisterManualEvents();
            base.OnViewModelUnbinding();
        }

        private void UpdateApiErrorState()
        {
            if (_viewModel.ErrorDetail != null)
            {
                FMDialog.ShowApiError(this, LocalizationSettings.StringDatabase.GetLocalizedString("UI", "ERROR_TITLE"), _viewModel.ErrorDetail, onOk: () =>
                {
                    if (_navController != null)
                    {
                        _navController.PopBackStack();
                    }
                    else
                    {
                        OnNavigationRequested(Actions.go_to_home, null);
                    }
                });
                _viewModel.ErrorDetail = null;
            }
        }
    }
}
