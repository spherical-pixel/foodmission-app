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
        private readonly Dictionary<string, FormFieldItemDropDownField> _dropdownsByDimension = new Dictionary<string, FormFieldItemDropDownField>();
        private readonly List<AccessibilityNode> _rowNodes = new List<AccessibilityNode>();

        protected override int StepCount => 1;

        // Six dropdowns are taller than the body on small phones: let step-body-scroll scroll them
        protected override bool GrowWithStepContent => true;

        protected override string CompleteButtonLabel =>
            _viewModel != null && _viewModel.Mode == DimensionLevelsMode.Proposal ? "@UI:TXT_CONTINUE" : "@UI:SAVE";

        // Decided in the constructor, before the mode is known: hidden again in Proposal mode (see ApplyMode)
        protected override bool ShowCloseButton => true;

        /// <summary>Edit goes straight back; the onboarding proposal confirms first, since leaving skips the rest of the onboarding.</summary>
        protected override void OnCloseRequested()
        {
            if (_viewModel == null || _viewModel.IsSubmitting)
            {
                return;
            }
            if (_viewModel.Mode != DimensionLevelsMode.Proposal)
            {
                _viewModel.Close();
                return;
            }
            OnboardingExitDialog.Show(this, OnboardingExitDialog.LevelsMessage, () =>
            {
                ResetNutriToIdle();
                _viewModel.Close();
            });
        }

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
            _ = _viewModel.EnsureDimensionNamesAsync();
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

            // Accessibility nodes were created inside base.OnEnter, before the rows existed
            if (_accessibilityHierarchy != null)
            {
                TeardownAccessibilityNodes();
                SetupAccessibilityNodes();
            }
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
            _dropdownsByDimension.Clear();
            bool proposal = _viewModel.Mode == DimensionLevelsMode.Proposal;

            // One card with a dropdown per dimension, like the onboarding profile steps
            var card = new ExVisualElement();
            card.AddToClassList("box-background");
            card.AddToClassList("fm-shadow-wrapper");
            card.AddToClassList("fm-dimension-levels__card");

            foreach (DimensionLevelRow row in _viewModel.Rows)
            {
                var dropdown = new FormFieldItemDropDownField
                {
                    name = "level-dropdown-" + row.DimensionCode.ToLowerInvariant(),
                    HeadingText = row.DimensionName
                };
                dropdown.AddToClassList("fm-dimension-levels__dropdown");
                dropdown.Dropdown.sourceItems = k_LevelLabels;
                dropdown.Dropdown.bindItem = (item, index) =>
                {
                    item.label = k_LevelLabels[index];
                    item.icon = null;
                };
                dropdown.Dropdown.SetValueWithoutNotify(new[] { Math.Max(0, ContentLevel.Rank(row.Level)) });

                string dimensionCode = row.DimensionCode;
                dropdown.Dropdown.RegisterValueChangedCallback(evt =>
                {
                    int index = evt.newValue?.FirstOrDefault() ?? -1;
                    if (index >= 0 && index < ContentLevel.All.Length)
                    {
                        _viewModel?.SetLevel(dimensionCode, ContentLevel.All[index]);
                    }
                });

                if (proposal)
                {
                    // The field's own help-text slot (reserved space under the dropdown), so nothing overlaps
                    dropdown.HelpTextText = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "DIMENSION_LEVELS_PROPOSED")
                        + ": " + LocalizationSettings.StringDatabase.GetLocalizedString("UI", "SEGMENT_" + row.ProposedLevel);
                }
                card.Add(dropdown);

                _dropdownsByDimension[dimensionCode] = dropdown;
            }

            _rowsContainer.Add(card);
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
                if (_dropdownsByDimension.TryGetValue(row.DimensionCode, out FormFieldItemDropDownField dropdown))
                {
                    dropdown.Dropdown.SetValueWithoutNotify(new[] { Math.Max(0, ContentLevel.Rank(row.Level)) });
                    dropdown.HeadingText = row.DimensionName;
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
                if (_dropdownsByDimension.TryGetValue(row.DimensionCode, out FormFieldItemDropDownField dropdown))
                {
                    node.frameGetter = MakeElementFrameGetter(dropdown);
                }
                _rowNodes.Add(node);
            }
        }

        protected override void TeardownAccessibilityNodes()
        {
            // Rebuilt after the rows exist (ApplyMode): remove the old nodes so they are not duplicated
            if (_accessibilityHierarchy != null)
            {
                foreach (AccessibilityNode node in _rowNodes)
                {
                    _accessibilityHierarchy.RemoveNode(node);
                }
            }
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
