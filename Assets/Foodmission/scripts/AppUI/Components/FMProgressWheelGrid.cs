using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

namespace eu.foodmission.platform.Components
{
    /// <summary>Grid of progress wheel cards. Falls back to one column (horizontal cards) when space is short or the text scale is large.</summary>
    [UxmlElement]
    public partial class FMProgressWheelGrid : VisualElement
    {
        public const string UssClassName = "fm-wheel-grid";
        private const string ColsClassPrefix = "fm-wheel-grid--cols-";

        private readonly List<FMProgressWheelCard> _cards = new List<FMProgressWheelCard>();
        private int _columns = 2;
        private float _minCardWidth = 420f;
        private bool _forceSingleColumn;
        private int _effectiveColumns = -1;

        [UxmlAttribute]
        public int columns
        {
            get => _columns;
            set { _columns = value; ApplyColumns(); }
        }

        [UxmlAttribute("min-card-width")]
        public float minCardWidth
        {
            get => _minCardWidth;
            set { _minCardWidth = value; ApplyColumns(); }
        }

        public bool ForceSingleColumn
        {
            get => _forceSingleColumn;
            set { _forceSingleColumn = value; ApplyColumns(); }
        }

        public int EffectiveColumns => _effectiveColumns;
        public IReadOnlyList<FMProgressWheelCard> Cards => _cards;

        public FMProgressWheelGrid()
        {
            AddToClassList(UssClassName);
            RegisterCallback<GeometryChangedEvent>(_ => ApplyColumns());
            ApplyColumns();
        }

        public void SetWheels(IReadOnlyList<ProgressWheel> wheels)
        {
            int count = wheels?.Count ?? 0;
            while (_cards.Count > count)
            {
                FMProgressWheelCard last = _cards[_cards.Count - 1];
                _cards.RemoveAt(_cards.Count - 1);
                last.RemoveFromHierarchy();
            }
            while (_cards.Count < count)
            {
                var card = new FMProgressWheelCard();
                _cards.Add(card);
                Add(card);
            }
            for (int i = 0; i < count; i++)
            {
                _cards[i].Bind(wheels[i]);
                _cards[i].SetHorizontal(_effectiveColumns == 1);
            }
        }

        public static int ResolveColumns(float width, int columns, float minCardWidth, bool forceSingleColumn)
        {
            int requested = Math.Max(1, columns);
            if (forceSingleColumn)
            {
                return 1;
            }
            if (float.IsNaN(width) || width <= 0f || minCardWidth <= 0f)
            {
                return requested;
            }
            for (int n = requested; n > 1; n--)
            {
                if (width / n >= minCardWidth)
                {
                    return n;
                }
            }
            return 1;
        }

        private void ApplyColumns()
        {
            int effective = ResolveColumns(resolvedStyle.width, _columns, _minCardWidth, _forceSingleColumn);
            if (effective == _effectiveColumns)
            {
                return;
            }

            if (_effectiveColumns > 0)
            {
                RemoveFromClassList(ColsClassPrefix + _effectiveColumns);
            }
            _effectiveColumns = effective;
            AddToClassList(ColsClassPrefix + effective);

            foreach (FMProgressWheelCard card in _cards)
            {
                card.SetHorizontal(effective == 1);
            }
        }
    }
}
