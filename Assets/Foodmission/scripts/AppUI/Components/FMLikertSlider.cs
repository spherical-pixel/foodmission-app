using System;
using System.Collections.Generic;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace eu.foodmission.platform.Components
{
    [UxmlElement]
    public partial class FMLikertSlider : ExVisualElement
    {
        private static Texture2D s_GradientTexture;

        private VisualElement _pointerContainer;
        private VisualElement _pointerTriangle;
        private VisualElement _gradientBar;
        private VisualElement _emojisContainer;
        private VisualElement _extremesRow;
        private Text _leftExtremeLabel;
        private Text _rightExtremeLabel;
        private Text _selectedLabel;

        private readonly List<VisualElement> _emojiElements = new List<VisualElement>();
        private AnswerOptionDto[] _options;
        private int _selectedIndex = -1; // -1 means unselected
        private bool _isDragging = false;

        public event Action<int> OnValueChanged;

        public int SelectedIndex => _selectedIndex;
        public int SelectedValue => (_selectedIndex >= 0 && _options != null && _selectedIndex < _options.Length)
            ? _options[_selectedIndex].value
            : -1;

        public FMLikertSlider()
        {
            //AddToClassList("fm-likert-slider");
            name = "FMLikertSlider";
            style.flexDirection = FlexDirection.Column;
            style.alignItems = Align.Center;
            style.width = new StyleLength(Length.Percent(100));
            style.marginTop = 12;
            style.marginBottom = 12;

            BuildUI();
        }

        private static Texture2D GetOrCreateGradientTexture()
        {
            if (s_GradientTexture != null) return s_GradientTexture;

            s_GradientTexture = new Texture2D(256, 1, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(new Color(0.86f, 0.16f, 0.12f), 0.0f),  // Deep Red
                    new GradientColorKey(new Color(0.98f, 0.55f, 0.08f), 0.25f), // Orange
                    new GradientColorKey(new Color(0.99f, 0.84f, 0.12f), 0.50f), // Yellow
                    new GradientColorKey(new Color(0.35f, 0.82f, 0.25f), 0.75f), // Green
                    new GradientColorKey(new Color(0.12f, 0.72f, 0.95f), 1.0f)   // Cyan/Blue
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1.0f, 0.0f),
                    new GradientAlphaKey(1.0f, 1.0f)
                }
            );

            for (int x = 0; x < 256; x++)
            {
                float t = x / 255f;
                s_GradientTexture.SetPixel(x, 0, gradient.Evaluate(t));
            }
            s_GradientTexture.Apply();
            return s_GradientTexture;
        }

        private void BuildUI()
        {
            this.style.marginTop = 80;
            // 1. Pointer Container
            _pointerContainer = new ExVisualElement();
            _pointerContainer.style.width = new StyleLength(Length.Percent(100));
            //_pointerContainer.style.height = 18;
            _pointerContainer.style.position = Position.Absolute;
            _pointerContainer.style.marginBottom = 4;


            _pointerTriangle = new ExVisualElement();
            _pointerTriangle.style.width = 0;
            _pointerTriangle.style.height = 0;
            _pointerTriangle.style.borderLeftWidth = 35;
            _pointerTriangle.style.borderRightWidth = 35;
            _pointerTriangle.style.borderTopWidth = 140;
            _pointerTriangle.style.borderLeftColor = Color.clear;
            _pointerTriangle.style.borderRightColor = Color.clear;
            _pointerTriangle.style.borderTopColor = Color.white;
            _pointerTriangle.style.position = Position.Absolute;
            _pointerTriangle.style.top = -35;
            _pointerTriangle.style.visibility = Visibility.Hidden; // Hidden when unselected
            _pointerTriangle.style.alignSelf = Align.Center;
            _pointerContainer.Add(_pointerTriangle);
            _pointerTriangle.AddToClassList("fm-shadow-wrapper");



            // 2. Gradient Bar with Emojis
            _gradientBar = new VisualElement();
            _gradientBar.style.width = new StyleLength(Length.Percent(100));
            _gradientBar.style.height = 145;
            _gradientBar.style.borderTopLeftRadius = 14;
            _gradientBar.style.borderTopRightRadius = 14;
            _gradientBar.style.borderBottomLeftRadius = 14;
            _gradientBar.style.borderBottomRightRadius = 14;
            _gradientBar.style.backgroundImage = new StyleBackground(GetOrCreateGradientTexture());
            _gradientBar.style.position = Position.Relative;
            _gradientBar.style.flexDirection = FlexDirection.Row;
            _gradientBar.style.alignItems = Align.Center;
            _gradientBar.style.justifyContent = Justify.SpaceAround;
            _gradientBar.style.paddingLeft = 8;
            _gradientBar.style.paddingRight = 8;

            _emojisContainer = new VisualElement();
            _emojisContainer.style.width = new StyleLength(Length.Percent(100));
            _emojisContainer.style.height = new StyleLength(Length.Percent(100));
            _emojisContainer.style.flexDirection = FlexDirection.Row;
            _emojisContainer.style.alignItems = Align.Center;
            _emojisContainer.style.justifyContent = Justify.SpaceAround;
            _gradientBar.Add(_emojisContainer);

            Add(_gradientBar);

            // Touch / Drag events on Gradient Bar
            _gradientBar.RegisterCallback<PointerDownEvent>(OnPointerDown);
            _gradientBar.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            _gradientBar.RegisterCallback<PointerUpEvent>(OnPointerUp);
            _gradientBar.RegisterCallback<PointerCaptureOutEvent>(e => _isDragging = false);

            // 3. Extremes Row (e.g. Strongly disagree / Strongly agree)
            _extremesRow = new VisualElement();
            _extremesRow.style.width = new StyleLength(Length.Percent(100));
            _extremesRow.style.flexDirection = FlexDirection.Row;
            _extremesRow.style.justifyContent = Justify.SpaceBetween;
            _extremesRow.style.marginTop = 6;
            _extremesRow.style.paddingLeft = 4;
            _extremesRow.style.paddingRight = 4;

            _leftExtremeLabel = new Text { size = TextSize.M };
            _leftExtremeLabel.style.color = new StyleColor(new Color(0.8f, 0.8f, 0.8f, 0.85f));
            _extremesRow.Add(_leftExtremeLabel);

            _rightExtremeLabel = new Text { size = TextSize.M };
            _rightExtremeLabel.style.color = new StyleColor(new Color(0.8f, 0.8f, 0.8f, 0.85f));
            _extremesRow.Add(_rightExtremeLabel);

            Add(_extremesRow);

            // 4. Selected Response Text Card
            var selectedBadge = new ExVisualElement();
            selectedBadge.style.width = new StyleLength(Length.Percent(100));
            selectedBadge.style.marginTop = 14;
            selectedBadge.style.minHeight = 44;
            selectedBadge.style.paddingTop = 8;
            selectedBadge.style.paddingBottom = 8;
            selectedBadge.style.paddingLeft = 14;
            selectedBadge.style.paddingRight = 14;
            selectedBadge.style.alignItems = Align.Center;
            selectedBadge.style.justifyContent = Justify.Center;

            _selectedLabel = new Text { size = TextSize.M };
            _selectedLabel.style.width = new StyleLength(Length.Percent(100));
            _selectedLabel.style.whiteSpace = WhiteSpace.Normal;
            _selectedLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _selectedLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _selectedLabel.style.color = new StyleColor(new Color(0.7f, 0.9f, 0.95f, 0.9f));
            _selectedLabel.style.visibility = Visibility.Hidden;
            _selectedLabel.text = "\u00A0";
            selectedBadge.Add(_selectedLabel);

            Add(selectedBadge);

            Add(_pointerContainer);

            // Default 5-point setup
            SetOptions(GetDefault5LikertOptions(), -1);
        }

        public void SetOptions(AnswerOptionDto[] options, int initialValue = -1)
        {
            _options = options ?? GetDefault5LikertOptions();
            _emojisContainer.Clear();
            _emojiElements.Clear();

            string[] defaultEmojis = _options.Length switch
            {
                5 => new[] { "😡", "🙁", "😐", "🙂", "😄" },
                7 => new[] { "😡", "😠", "🙁", "😐", "🙂", "😊", "😄" },
                3 => new[] { "🙁", "😐", "🙂" },
                _ => new[] { "😡", "🙁", "😐", "🙂", "😄" }
            };

            for (int i = 0; i < _options.Length; i++)
            {
                int index = i;
                var emojiWrapper = new VisualElement();
                emojiWrapper.style.flexGrow = 1;
                emojiWrapper.style.alignItems = Align.Center;
                emojiWrapper.style.justifyContent = Justify.Center;
                emojiWrapper.style.height = new StyleLength(Length.Percent(100));

                var emojiLabel = new Text
                {
                    text = i < defaultEmojis.Length ? defaultEmojis[i] : "😐",
                    size = TextSize.L
                };
                emojiLabel.style.fontSize = 72;
                emojiLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                emojiWrapper.Add(emojiLabel);

                emojiWrapper.RegisterCallback<ClickEvent>(e =>
                {
                    SelectIndex(index, triggerEvent: true);
                });

                _emojisContainer.Add(emojiWrapper);
                _emojiElements.Add(emojiWrapper);
            }

            // Extreme labels
            if (_options.Length > 0)
            {
                _leftExtremeLabel.text = _options[0]?.label ?? "";
                _rightExtremeLabel.text = _options[_options.Length - 1]?.label ?? "";
            }

            // Match initial value
            int selectedIdx = -1;
            if (initialValue > 0)
            {
                for (int i = 0; i < _options.Length; i++)
                {
                    if (_options[i].value == initialValue)
                    {
                        selectedIdx = i;
                        break;
                    }
                }
            }

            SelectIndex(selectedIdx, triggerEvent: false);
        }

        public void SelectValue(int value, bool triggerEvent = true)
        {
            int foundIdx = -1;
            if (_options != null && value > 0)
            {
                for (int i = 0; i < _options.Length; i++)
                {
                    if (_options[i].value == value)
                    {
                        foundIdx = i;
                        break;
                    }
                }
            }
            SelectIndex(foundIdx, triggerEvent);
        }

        public void SelectIndex(int index, bool triggerEvent = true)
        {
            _selectedIndex = index;

            if (_selectedIndex < 0 || _options == null || _selectedIndex >= _options.Length)
            {
                // Unselected state
                _pointerTriangle.style.visibility = Visibility.Hidden;
                _selectedLabel.style.visibility = Visibility.Hidden;
                _selectedLabel.text = "\u00A0";
                HighlightEmoji(-1);
            }
            else
            {
                _pointerTriangle.style.visibility = Visibility.Visible;
                _selectedLabel.style.visibility = Visibility.Visible;
                _selectedLabel.text = _options[_selectedIndex].label ?? "";
                HighlightEmoji(_selectedIndex);
                UpdatePointerPosition();
            }

            if (triggerEvent)
            {
                OnValueChanged?.Invoke(SelectedValue);
            }
        }

        private void HighlightEmoji(int activeIndex)
        {
            for (int i = 0; i < _emojiElements.Count; i++)
            {
                var el = _emojiElements[i];
                if (i == activeIndex)
                {
                    el.style.scale = new StyleScale(new Scale(new Vector2(1.25f, 1.25f)));
                    el.style.opacity = 1.0f;
                }
                else
                {
                    el.style.scale = new StyleScale(new Scale(Vector2.one));
                    el.style.opacity = activeIndex >= 0 ? 0.75f : 1.0f;
                }
            }
        }

        private void UpdatePointerPosition()
        {
            if (_selectedIndex < 0 || _selectedIndex >= _emojiElements.Count)
                return;

            schedule.Execute(() =>
            {
                float containerWidth = _gradientBar.resolvedStyle.width;
                if (containerWidth <= 0)
                {
                    containerWidth = resolvedStyle.width;
                }

                if (containerWidth > 0 && _options.Length > 0)
                {
                    float slotWidth = containerWidth / _options.Length;
                    float targetCenterX = (slotWidth * _selectedIndex) + (slotWidth / 2f);
                    float pointerWidth = 70f;
                    _pointerTriangle.style.left = targetCenterX - (pointerWidth / 2f);
                }
            });
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            _isDragging = true;
            _gradientBar.CapturePointer(evt.pointerId);
            UpdateSelectionFromPointer(evt.localPosition.x);
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (_isDragging && _gradientBar.HasPointerCapture(evt.pointerId))
            {
                UpdateSelectionFromPointer(evt.localPosition.x);
            }
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (_isDragging)
            {
                _isDragging = false;
                if (_gradientBar.HasPointerCapture(evt.pointerId))
                {
                    _gradientBar.ReleasePointer(evt.pointerId);
                }
                UpdateSelectionFromPointer(evt.localPosition.x);
            }
        }

        private void UpdateSelectionFromPointer(float localX)
        {
            if (_options == null || _options.Length == 0) return;

            float width = _gradientBar.resolvedStyle.width;
            if (width <= 0) return;

            float clampedX = Mathf.Clamp(localX, 0, width);
            float normalized = clampedX / width;
            int targetIndex = Mathf.Clamp(Mathf.FloorToInt(normalized * _options.Length), 0, _options.Length - 1);

            if (targetIndex != _selectedIndex)
            {
                SelectIndex(targetIndex, triggerEvent: true);
            }
        }

        private static AnswerOptionDto[] GetDefault5LikertOptions()
        {
            return new[]
            {
                new AnswerOptionDto { value = 1, label = "Totalmente en desacuerdo" },
                new AnswerOptionDto { value = 2, label = "En desacuerdo" },
                new AnswerOptionDto { value = 3, label = "Ni de acuerdo ni en desacuerdo" },
                new AnswerOptionDto { value = 4, label = "De acuerdo" },
                new AnswerOptionDto { value = 5, label = "Totalmente de acuerdo" }
            };
        }
    }
}
