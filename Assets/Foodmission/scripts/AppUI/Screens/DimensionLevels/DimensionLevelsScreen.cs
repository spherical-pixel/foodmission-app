using System;
using System.Collections.Generic;
using System.Linq;

using eu.foodmission.platform.Components;
using Unity.AppUI.Navigation;
using Unity.AppUI.UI;

using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.Localization.Settings;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace eu.foodmission.platform
{
    /// <summary>
    /// One-step flow with the user's level per dimension: the proposal after the onboarding survey,
    /// or editing the levels from Edit Profile / a locked-content dialog.
    /// </summary>
    [Preserve]
    public class DimensionLevelsScreen : StepFlowScreenBase<DimensionLevelsViewModel>
    {
        private static readonly string[] k_LevelLabels = { "@UI:SEGMENT_BEGINNER", "@UI:SEGMENT_INTERMEDIATE", "@UI:SEGMENT_ADVANCED" };

        private FMNutriView _nutriView;
        private ExVisualElement _messageCard;
        private Unity.AppUI.UI.Text _messageText;
        private VisualElement _rowsContainer;
        private Unity.AppUI.UI.Button _resetButton;
        private readonly Dictionary<string, ActionGroup> _groupsByDimension = new Dictionary<string, ActionGroup>();
        private readonly List<AccessibilityNode> _rowNodes = new List<AccessibilityNode>();

        protected override int StepCount => 1;

        protected override string CompleteButtonLabel =>
            _viewModel != null && _viewModel.Mode == DimensionLevelsMode.Proposal ? "@UI:TXT_CONTINUE" : "@UI:SAVE";

        // Decided in the constructor, before the mode is known: hidden again in Proposal mode (see ApplyMode)
        protected override bool ShowCloseButton => true;

        public override void OnEnter(NavController controller, NavDestination destination, Argument[] args)
        {
            base.OnEnter(controller, destination, args);

            if (_viewModel == null)
            {
                return;
            }

            // StepFlowScreenBase initializes the view model inside base.OnEnter, before the arguments are read
            _viewModel.Mode = DimensionLevelsMode.Edit;
            _viewModel.FromHome = false;
            if (args != null)
            {
                foreach (Argument arg in args)
                {
                    if (arg.name == DimensionLevelsNavigation.ModeArgument)
                    {
                        _viewModel.Mode = arg.value?.ToString() == DimensionLevelsNavigation.ProposalValue
                            ? DimensionLevelsMode.Proposal
                            : DimensionLevelsMode.Edit;
                    }
                    else if (arg.name == "fromHome")
                    {
                        _viewModel.FromHome = arg.value?.ToString() == "true";
                    }
                }
            }

            _viewModel.Reload();
            ApplyMode();
        }

        protected override void OnViewModelBound()
        {
            base.OnViewModelBound();
            if (_viewModel != null)
            {
                _viewModel.RowsChanged += OnRowsChanged;
            }
        }

        protected override void OnViewModelUnbinding()
        {
            if (_viewModel != null)
            {
                _viewModel.RowsChanged -= OnRowsChanged;
            }
            base.OnViewModelUnbinding();
        }

        private void ApplyMode()
        {
            bool proposal = _viewModel.Mode == DimensionLevelsMode.Proposal;

            VisualElement closeButton = contentContainer.Q<VisualElement>("btn-close");
            if (closeButton != null)
            {
                closeButton.style.display = proposal ? DisplayStyle.None : DisplayStyle.Flex;
            }

            FMButton nextButton = contentContainer.Q<FMButton>("btn-next");
            if (nextButton != null)
            {
                nextButton.title = CompleteButtonLabel;
            }

            if (_messageText != null && _messageCard != null)
            {
                _messageText.text = proposal ? "@UI:DIMENSION_LEVELS_NUTRI_PROPOSAL" : "@UI:DIMENSION_LEVELS_NUTRI_EDIT";
                _messageCard.style.display = DisplayStyle.Flex;
                _messageCard.AddToClassList("fm-step-flow__guide-card--visible");
            }

            BuildRows();
        }

        protected override void SetupCompanionSlot(VisualElement slot)
        {
            _nutriView = new FMNutriView();
            _nutriView.AddToClassList("fm-step-flow__guide-nutri");
            slot.Add(_nutriView);

            _messageCard = new ExVisualElement();
            _messageCard.AddToClassList("box-background");
            _messageCard.AddToClassList("fm-shadow-wrapper");
            _messageCard.AddToClassList("fm-step-flow__guide-card");

            _messageText = new Unity.AppUI.UI.Text { text = "" };
            _messageText.style.whiteSpace = WhiteSpace.Normal;
            _messageText.primary = false;
            _messageCard.Add(_messageText);

            _messageCard.style.display = DisplayStyle.None;
            slot.Add(_messageCard);
        }

        protected override VisualElement CreateStepContent(int stepIndex)
        {
            var root = new VisualElement();
            root.AddToClassList("fm-dimension-levels");

            _rowsContainer = new VisualElement();
            _rowsContainer.AddToClassList("fm-dimension-levels__rows");
            root.Add(_rowsContainer);

            _resetButton = new Unity.AppUI.UI.Button
            {
                title = "@UI:DIMENSION_LEVELS_RESET",
                variant = ButtonVariant.Default,
                quiet = true
            };
            _resetButton.AddToClassList("fm-dimension-levels__reset");
            _resetButton.clicked += () => _viewModel?.ResetToProposal();
            _resetButton.style.display = DisplayStyle.None;
            root.Add(_resetButton);

            return root;
        }

        private void BuildRows()
        {
            if (_rowsContainer == null || _viewModel == null)
            {
                return;
            }

            _rowsContainer.Clear();
            _groupsByDimension.Clear();
            bool proposal = _viewModel.Mode == DimensionLevelsMode.Proposal;

            foreach (DimensionLevelRow row in _viewModel.Rows)
            {
                var card = new ExVisualElement();
                card.AddToClassList("box-background");
                card.AddToClassList("fm-shadow-wrapper");
                card.AddToClassList("fm-dimension-levels__row");

                var name = new Unity.AppUI.UI.Text { text = row.DimensionName };
                name.AddToClassList("fm-dimension-levels__name");
                card.Add(name);

                var group = new ActionGroup
                {
                    selectionType = SelectionType.Single,
                    allowNoSelection = false,
                    compact = true,
                    justified = true
                };
                group.AddToClassList("fm-missions-action-group");
                group.AddToClassList("fm-dimension-levels__group");
                for (int i = 0; i < ContentLevel.All.Length; i++)
                {
                    group.Add(new ActionButton { label = k_LevelLabels[i] });
                }
                group.SetSelectionWithoutNotify(new[] { Math.Max(0, ContentLevel.Rank(row.Level)) });

                string dimensionCode = row.DimensionCode;
                group.selectionChanged += indices =>
                {
                    int index = indices?.FirstOrDefault() ?? -1;
                    if (index >= 0 && index < ContentLevel.All.Length)
                    {
                        _viewModel?.SetLevel(dimensionCode, ContentLevel.All[index]);
                    }
                };
                card.Add(group);

                if (proposal)
                {
                    var proposed = new Unity.AppUI.UI.Text
                    {
                        text = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "DIMENSION_LEVELS_PROPOSED")
                               + ": " + LocalizationSettings.StringDatabase.GetLocalizedString("UI", "SEGMENT_" + row.ProposedLevel)
                    };
                    proposed.AddToClassList("fm-dimension-levels__proposed");
                    card.Add(proposed);
                }

                _groupsByDimension[dimensionCode] = group;
                _rowsContainer.Add(card);
            }

            UpdateResetButton();
        }

        private void OnRowsChanged()
        {
            if (_viewModel == null)
            {
                return;
            }

            foreach (DimensionLevelRow row in _viewModel.Rows)
            {
                if (_groupsByDimension.TryGetValue(row.DimensionCode, out ActionGroup group))
                {
                    group.SetSelectionWithoutNotify(new[] { Math.Max(0, ContentLevel.Rank(row.Level)) });
                }
            }
            UpdateResetButton();
            RefreshRowNodeValues();
        }

        private void UpdateResetButton()
        {
            if (_resetButton == null || _viewModel == null)
            {
                return;
            }
            bool show = _viewModel.Mode == DimensionLevelsMode.Proposal && _viewModel.IsDifferentFromProposal;
            _resetButton.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // ── Accessibility ──

        protected override void SetupAccessibilityNodes()
        {
            base.SetupAccessibilityNodes();
            if (_accessibilityHierarchy == null || _viewModel == null)
            {
                return;
            }

            _rowNodes.Clear();
            foreach (DimensionLevelRow row in _viewModel.Rows)
            {
                AccessibilityNode node = _accessibilityHierarchy.AddNode(row.DimensionName);
                node.role = AccessibilityRole.StaticText;
                node.value = LevelLabel(row.Level);
                if (_groupsByDimension.TryGetValue(row.DimensionCode, out ActionGroup group))
                {
                    node.frameGetter = MakeElementFrameGetter(group);
                }
                _rowNodes.Add(node);
            }
        }

        protected override void TeardownAccessibilityNodes()
        {
            _rowNodes.Clear();
            base.TeardownAccessibilityNodes();
        }

        private void RefreshRowNodeValues()
        {
            if (_viewModel == null)
            {
                return;
            }
            for (int i = 0; i < _rowNodes.Count && i < _viewModel.Rows.Count; i++)
            {
                _rowNodes[i].value = LevelLabel(_viewModel.Rows[i].Level);
            }
        }

        private static string LevelLabel(string level)
        {
            return LocalizationSettings.StringDatabase.GetLocalizedString("UI", "SEGMENT_" + level);
        }

        private static Func<Rect> MakeElementFrameGetter(VisualElement element)
        {
            return () =>
            {
                if (element == null || element.panel == null)
                {
                    return Rect.zero;
                }
                var rect = element.worldBound;
                var scale = element.panel.scaledPixelsPerPoint;
                return new Rect(rect.position * scale, rect.size * scale);
            };
        }
    }
}
