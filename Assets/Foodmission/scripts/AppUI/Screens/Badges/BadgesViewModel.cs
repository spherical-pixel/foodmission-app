using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Unity.AppUI.MVVM;

using UnityEngine;

namespace eu.foodmission.platform
{
    /// <summary>Display data for one badge card.</summary>
    public class BadgeItem
    {
        public string Code;
        public string Name;
        public string Description;
        public bool Earned;
        public DateTime? EarnedAt;
        /// <summary>0..1, for LinearProgress.</summary>
        public float Progress;
        public string SpriteAddress;
        public bool ShowProgress;
    }

    [ObservableObject]
    public partial class BadgesViewModel : ViewModelBase
    {
        private readonly IBadgeService _badgeService;

        [ObservableProperty]
        private List<BadgeItem> _badges = new();

        [ObservableProperty]
        private int _earnedCount;

        [ObservableProperty]
        private int _totalCount;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private ApiErrorResponse _errorDetail;

        public BadgesViewModel(IStoreService storeService, IBadgeService badgeService)
            : base(storeService)
        {
            _badgeService = badgeService;
        }

        public async Task LoadBadgesAsync()
        {
            IsLoading = true;
            try
            {
                var (response, error) = await _badgeService.GetMyBadgesAsync();
                if (error != null || response == null)
                {
                    ErrorDetail = error;
                    Badges = new List<BadgeItem>();
                    return;
                }

                ErrorDetail = null;
                UserBadge[] source = response.badges ?? Array.Empty<UserBadge>();

                Badges = source
                    .Where(b => b != null && !string.IsNullOrEmpty(b.code))
                    .OrderBy(b => b.sortOrder)
                    .ThenBy(b => b.code, StringComparer.Ordinal)
                    .Select(ToItem)
                    .ToList();
                EarnedCount = Badges.Count(b => b.Earned);
                TotalCount = Badges.Count;

                string[] earnedCodes = Badges.Where(b => b.Earned).Select(b => b.Code).ToArray();
                _storeService?.store?.Dispatch(AppActions.setBadges.Invoke(earnedCodes));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] LoadBadgesAsync error: {ex.Message}");
                Badges = new List<BadgeItem>();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private static BadgeItem ToItem(UserBadge b)
        {
            return new BadgeItem
            {
                Code = b.code,
                Name = b.name,
                Description = b.description,
                Earned = b.earned,
                EarnedAt = b.earnedAt,
                Progress = Mathf.Clamp01(b.progress / 100f),
                SpriteAddress = BadgeSprites.Address(b.code, b.earned),
                ShowProgress = !b.earned
            };
        }
    }
}
