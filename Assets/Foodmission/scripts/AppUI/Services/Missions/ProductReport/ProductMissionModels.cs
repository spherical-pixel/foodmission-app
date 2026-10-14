using System;
using System.Collections.Generic;
using System.Linq;

namespace eu.foodmission.platform
{
    public enum ProductMissionCardKind
    {
        /// <summary>One event per food: a single "I've looked at it" button.</summary>
        Single,
        /// <summary>Several events, any of them (ProductChoice).</summary>
        Choice,
        /// <summary>Facts or values that must satisfy a ChecklistRule (ProductFacts, ProductValues).</summary>
        Checklist
    }

    public sealed class ProductMissionOption
    {
        public string LabelKey { get; }
        public string EventType { get; }
        /// <summary>Array value (labels of M.A2.2), null for event options.</summary>
        public string Value { get; }
        public IReadOnlyDictionary<string, object> Metadata { get; }
        public ProductFocus Focus { get; }

        public ProductMissionOption(string labelKey, string eventType, string value, IReadOnlyDictionary<string, object> metadata, ProductFocus focus)
        {
            LabelKey = labelKey;
            EventType = eventType;
            Value = value;
            Metadata = metadata;
            Focus = focus;
        }
    }

    /// <summary>What one mission asks about the food open in the product detail.</summary>
    public sealed class ProductMissionCard
    {
        public string MissionCode { get; }
        public string MissionTitle { get; }
        /// <summary>Events count from this moment: older reports don't lock, and the idempotency key is per mission window.</summary>
        public DateTime? MissionStartLocal { get; }
        public FoodInfoType FoodType { get; }
        public string FoodId { get; }
        public float ProgressPercent { get; }
        public MissionReportStep Step { get; }
        public ProductMissionCardKind Kind { get; }
        /// <summary>Metadata field that carries the food id (productId or genericFoodId).</summary>
        public string IdField { get; }
        public IReadOnlyList<ProductMissionOption> Options { get; }
        /// <summary>Options already sent for this food: shown checked and disabled.</summary>
        public IReadOnlyCollection<int> Locked { get; }
        /// <summary>Options whose data the product detail shows (outlined, never pre-selected).</summary>
        public IReadOnlyCollection<int> Suggested { get; }
        public ProductFocus Focus { get; }
        public bool AllSent => Options.Count > 0 && Locked.Count == Options.Count;

        public ProductMissionCard(string missionCode, string missionTitle, DateTime? missionStartLocal, FoodInfoType foodType, string foodId,
            float progressPercent, MissionReportStep step, ProductMissionCardKind kind, string idField, IReadOnlyList<ProductMissionOption> options,
            IReadOnlyCollection<int> locked, IReadOnlyCollection<int> suggested)
        {
            MissionCode = missionCode;
            MissionTitle = missionTitle;
            MissionStartLocal = missionStartLocal;
            FoodType = foodType;
            FoodId = foodId;
            ProgressPercent = progressPercent;
            Step = step;
            Kind = kind;
            IdField = idField;
            Options = options ?? Array.Empty<ProductMissionOption>();
            Locked = locked ?? Array.Empty<int>();
            Suggested = suggested ?? Array.Empty<int>();
            Focus = Options.Aggregate(ProductFocus.None, (focus, option) => focus | option.Focus);
        }
    }

    public sealed class ProductMissionInputs
    {
        public readonly List<CheckInMission> Missions = new List<CheckInMission>();
        public readonly Dictionary<string, MissionProgress> Progress = new Dictionary<string, MissionProgress>(StringComparer.OrdinalIgnoreCase);
        public FoodInfoType FoodType;
        public string FoodId;
        /// <summary>Missions past their deadline get no card (as in the check-in).</summary>
        public DateTime NowLocal = DateTime.Now;
        /// <summary>Event type → value ("" for events without an array field) → latest local time it was sent for this food.</summary>
        public readonly Dictionary<string, Dictionary<string, DateTime>> Sent = new Dictionary<string, Dictionary<string, DateTime>>(StringComparer.Ordinal);
        /// <summary>Data the product detail shows (FoodInfoViewModel.AvailableFocusKeys).</summary>
        public IReadOnlyCollection<string> AvailableKeys = Array.Empty<string>();
    }
}
