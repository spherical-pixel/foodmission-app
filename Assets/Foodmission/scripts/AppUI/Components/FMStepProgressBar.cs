using System;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Properties;

namespace eu.foodmission.platform.Components
{
    public enum StepProgressMode
    {
        Compact,
        Detailed
    }

    /// <summary>
    /// A custom visual element showing progress through a multi-step flow.
    /// Supports both Compact (progress bar + text) and Detailed (circles/dots + optional labels) modes.
    /// </summary>
    [UxmlElement]
    public partial class FMStepProgressBar : VisualElement
    {
        public const string ussClassName = "fm-step-progress-bar";
        public const string compactUssClassName = ussClassName + "--compact";
        public const string detailedUssClassName = ussClassName + "--detailed";

        private int _stepCount = 3;
        private int _currentStep = 0;
        private StepProgressMode _mode = StepProgressMode.Compact;
        private string[] _labels = Array.Empty<string>();

        // Internal UI Elements
        private readonly VisualElement _container;

        [UxmlAttribute("step-count")] [CreateProperty]
        public int StepCount
        {
            get => _stepCount;
            set
            {
                var val = Mathf.Max(1, value);
                if (_stepCount != val)
                {
                    _stepCount = val;
                    _currentStep = Mathf.Clamp(_currentStep, 0, _stepCount - 1);
                    Rebuild();
                    this.NotifyPropertyChanged(nameof(StepCount));
                }
            }
        }

        [UxmlAttribute("current-step")] [CreateProperty]
        public int CurrentStep
        {
            get => _currentStep;
            set
            {
                var clamped = Mathf.Clamp(value, 0, Mathf.Max(0, StepCount - 1));
                if (_currentStep != clamped)
                {
                    _currentStep = clamped;
                    UpdateProgress();
                    this.NotifyPropertyChanged(nameof(CurrentStep));
                }
            }
        }

        [UxmlAttribute("mode")] [CreateProperty]
        public StepProgressMode Mode
        {
            get => _mode;
            set
            {
                if (_mode != value)
                {
                    _mode = value;
                    Rebuild();
                    this.NotifyPropertyChanged(nameof(Mode));
                }
            }
        }

        [UxmlAttribute("labels")] [CreateProperty]
        public string[] Labels
        {
            get => _labels ?? Array.Empty<string>();
            set
            {
                _labels = value ?? Array.Empty<string>();
                if (_mode == StepProgressMode.Detailed)
                {
                    Rebuild();
                }
                this.NotifyPropertyChanged(nameof(Labels));
            }
        }

        public FMStepProgressBar()
        {
            AddToClassList(ussClassName);
            _container = new VisualElement { name = "progress-container" };
            _container.style.flexGrow = 1;
            Add(_container);
            
            Rebuild();
        }

        private void Rebuild()
        {
            _container.Clear();
            RemoveFromClassList(compactUssClassName);
            RemoveFromClassList(detailedUssClassName);

            if (_mode == StepProgressMode.Compact)
            {
                AddToClassList(compactUssClassName);
                BuildCompactMode();
            }
            else
            {
                AddToClassList(detailedUssClassName);
                BuildDetailedMode();
            }
        }

        private void BuildCompactMode()
        {
            // Title text: "Step X of Y"
            var title = new Unity.AppUI.UI.Text { name = "progress-text" };
            title.AddToClassList("fm-step-progress__text");
            _container.Add(title);

            // Progress bar track
            var track = new VisualElement { name = "progress-track" };
            track.AddToClassList("fm-step-progress__track");
            
            // Progress bar fill
            var fill = new VisualElement { name = "progress-fill" };
            fill.AddToClassList("fm-step-progress__fill");
            track.Add(fill);
            
            _container.Add(track);

            UpdateProgress();
        }

        private void BuildDetailedMode()
        {
            // Horizontal row of step circles with lines in between
            var stepsRow = new VisualElement { name = "steps-row" };
            stepsRow.AddToClassList("fm-step-progress__row");

            for (int i = 0; i < _stepCount; i++)
            {
                // Line connecting to the previous step (if not the first step)
                if (i > 0)
                {
                    var line = new VisualElement();
                    line.AddToClassList("fm-step-progress__line");
                    stepsRow.Add(line);
                }

                // Step circle
                var stepCircle = new VisualElement();
                stepCircle.AddToClassList("fm-step-progress__circle");
                
                var numberText = new Unity.AppUI.UI.Text { text = (i + 1).ToString() };
                numberText.AddToClassList("fm-step-progress__circle-text");
                stepCircle.Add(numberText);

                stepsRow.Add(stepCircle);
            }

            _container.Add(stepsRow);

            // Optional labels row below the steps
            if (Labels != null && Labels.Length > 0)
            {
                var labelsRow = new VisualElement { name = "labels-row" };
                labelsRow.AddToClassList("fm-step-progress__labels-row");

                for (int i = 0; i < _stepCount; i++)
                {
                    var labelText = i < Labels.Length ? Labels[i] : "";
                    var label = new Unity.AppUI.UI.Text { text = labelText };
                    label.AddToClassList("fm-step-progress__label");
                    labelsRow.Add(label);
                }
                _container.Add(labelsRow);
            }

            UpdateProgress();
        }

        private void UpdateProgress()
        {
            if (_mode == StepProgressMode.Compact)
            {
                var title = _container.Q<Unity.AppUI.UI.Text>("progress-text");
                if (title != null)
                {
                    title.text = $"Step {_currentStep + 1} of {_stepCount}";
                }

                var fill = _container.Q<VisualElement>("progress-fill");
                if (fill != null)
                {
                    float percent = _stepCount > 0 ? (_currentStep + 1) / (float)_stepCount : 1.0f;
                    fill.style.width = Length.Percent(percent * 100f);
                }
            }
            else
            {
                // Update detailed circles, lines, and labels
                var stepsRow = _container.Q<VisualElement>("steps-row");
                if (stepsRow != null)
                {
                    var children = stepsRow.Children();
                    int circleIndex = 0;
                    foreach (var child in children)
                    {
                        if (child.ClassListContains("fm-step-progress__circle"))
                        {
                            child.RemoveFromClassList("active");
                            child.RemoveFromClassList("completed");
                            child.RemoveFromClassList("pending");

                            if (circleIndex == _currentStep)
                            {
                                child.AddToClassList("active");
                            }
                            else if (circleIndex < _currentStep)
                            {
                                child.AddToClassList("completed");
                            }
                            else
                            {
                                child.AddToClassList("pending");
                            }
                            circleIndex++;
                        }
                        else if (child.ClassListContains("fm-step-progress__line"))
                        {
                            child.RemoveFromClassList("active");
                            child.RemoveFromClassList("pending");
                            
                            if (circleIndex <= _currentStep)
                            {
                                child.AddToClassList("active");
                            }
                            else
                            {
                                child.AddToClassList("pending");
                            }
                        }
                    }
                }

                var labelsRow = _container.Q<VisualElement>("labels-row");
                if (labelsRow != null)
                {
                    var labels = labelsRow.Query<Unity.AppUI.UI.Text>().ToList();
                    for (int i = 0; i < labels.Count; i++)
                    {
                        labels[i].RemoveFromClassList("active");
                        labels[i].RemoveFromClassList("completed");
                        labels[i].RemoveFromClassList("pending");

                        if (i == _currentStep)
                        {
                            labels[i].AddToClassList("active");
                        }
                        else if (i < _currentStep)
                        {
                            labels[i].AddToClassList("completed");
                        }
                        else
                        {
                            labels[i].AddToClassList("pending");
                        }
                    }
                }
            }
        }
    }
}
