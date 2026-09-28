using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.AppUI.MVVM;
using UnityEngine;

namespace eu.foodmission.platform
{
    [ObservableObject]
    public partial class NutriEditorViewModel : ViewModelBase
    {
        private readonly IFoodyService _foodyService;
        private readonly INutriService _nutriService;

        public INutriService NutriService => _nutriService;

        private FoodyLoadout _initialLoadout = new();
        private FoodyLoadout _workingLoadout = new();
        private List<FoodyItem> _catalogItems = new();

        public NutriEditorViewModel(IStoreService storeService, IFoodyService foodyService, INutriService nutriService)
            : base(storeService)
        {
            _foodyService = foodyService;
            _nutriService = nutriService;
        }

        public async Task InitializeAsync()
        {
            if (_nutriService?.CurrentLoadout != null)
            {
                _initialLoadout = CopyLoadout(_nutriService.CurrentLoadout);
                _workingLoadout = CopyLoadout(_nutriService.CurrentLoadout);
            }

            var (loadout, loadoutErr) = await _foodyService.GetLoadoutAsync();
            if (loadoutErr == null && loadout != null)
            {
                bool hasRemote = loadout.antennas != null || loadout.ears != null || loadout.glasses != null;
                bool hasLocal = _workingLoadout.antennas != null || _workingLoadout.ears != null || _workingLoadout.glasses != null;

                if (hasRemote || !hasLocal)
                {
                    _initialLoadout = CopyLoadout(loadout);
                    _workingLoadout = CopyLoadout(loadout);
                }
            }

            var (items, itemsErr) = await _foodyService.GetItemsAsync();
            if (itemsErr == null && items != null)
            {
                _catalogItems = items;
            }

            _nutriService?.ApplyLoadout(_workingLoadout);
        }

        public List<FoodyItem> GetItemsForCategory(string category)
        {
            string norm = FoodyItemType.Normalize(category);
            return _catalogItems.Where(i => FoodyItemType.Normalize(i.type) == norm).OrderBy(i => i.slot).ToList();
        }

        public int GetEquippedSlot(string category)
        {
            return _workingLoadout?.GetItemByType(category)?.slot ?? 0;
        }

        public void EquipItem(string category, int slot)
        {
            string norm = FoodyItemType.Normalize(category);
            var item = _catalogItems.FirstOrDefault(i => FoodyItemType.Normalize(i.type) == norm && i.slot == slot)
                       ?? new FoodyItem { code = $"{norm}_{slot}", type = norm, slot = slot, owned = true };

            _workingLoadout.SetItemByType(norm, item);
            _nutriService?.EquipItem(norm, slot);
        }

        public void UnequipCategory(string category)
        {
            string norm = FoodyItemType.Normalize(category);
            _workingLoadout.SetItemByType(norm, null);
            _nutriService?.UnequipItem(norm);
        }

        public int UserPoints => _storeService?.GetAppState()?.userPoints ?? 0;

        public async Task<bool> PurchaseItemAsync(FoodyItem item)
        {
            if (item == null) return false;
            int userPoints = UserPoints;
            if (userPoints < item.cost) return false;

            var (resp, err) = await _foodyService.PurchaseItemAsync(item.code);
            if (err == null && resp != null)
            {
                item.owned = true;
                return true;
            }
            return false;
        }

        public async Task SaveAsync()
        {
            string[] categories = { FoodyItemType.Antennas, FoodyItemType.Glasses, FoodyItemType.Ears };
            foreach (var cat in categories)
            {
                int initSlot = _initialLoadout.GetItemByType(cat)?.slot ?? 0;
                int workSlot = _workingLoadout.GetItemByType(cat)?.slot ?? 0;

                if (initSlot != workSlot)
                {
                    if (workSlot > 0)
                    {
                        var workItem = _workingLoadout.GetItemByType(cat);
                        if (workItem != null) await _foodyService.EquipItemAsync(workItem.code);
                    }
                    else if (initSlot > 0)
                    {
                        var initItem = _initialLoadout.GetItemByType(cat);
                        if (initItem != null) await _foodyService.UnequipItemAsync(initItem.code);
                    }
                }
            }
            _nutriService?.ApplyLoadout(_workingLoadout);
            _initialLoadout = CopyLoadout(_workingLoadout);
        }

        public void ExitWithoutSaving()
        {
            _nutriService?.ApplyLoadout(_initialLoadout);
        }

        private static FoodyLoadout CopyLoadout(FoodyLoadout source)
        {
            if (source == null) return new FoodyLoadout();
            return new FoodyLoadout
            {
                antennas = source.antennas != null ? new FoodyItem { code = source.antennas.code, type = source.antennas.type, slot = source.antennas.slot, owned = source.antennas.owned } : null,
                glasses = source.glasses != null ? new FoodyItem { code = source.glasses.code, type = source.glasses.type, slot = source.glasses.slot, owned = source.glasses.owned } : null,
                ears = source.ears != null ? new FoodyItem { code = source.ears.code, type = source.ears.type, slot = source.ears.slot, owned = source.ears.owned } : null
            };
        }
    }
}
