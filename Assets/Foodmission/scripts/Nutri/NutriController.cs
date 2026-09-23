using System;
using System.Collections.Generic;
using UnityEngine;

namespace eu.foodmission.platform
{
    public class NutriController : MonoBehaviour
    {
        [SerializeField] private NutriAnimationController _nutriAnimationController;

        public NutriAnimationController NutriAnimationController => _nutriAnimationController;

        [SerializeField] private Camera _nutriCamera;
        public Camera NutriCamera => _nutriCamera;

        private readonly Dictionary<(string type, int slot), GameObject> _accessoryMap = new();
        private bool _accessoriesCached;

        private void Awake()
        {
            EnsureAccessoriesCached();
        }

        public void EnsureAccessoriesCached()
        {
            if (_accessoriesCached) return;
            _accessoriesCached = true;

            var allTransforms = GetComponentsInChildren<Transform>(true);
            foreach (var t in allTransforms)
            {
                string name = t.gameObject.name;

                // Match Antennas1..6
                if (name.StartsWith("Antennas", StringComparison.OrdinalIgnoreCase) &&
                    int.TryParse(name.Substring("Antennas".Length), out int antSlot))
                {
                    _accessoryMap[(FoodyItemType.Antennas, antSlot)] = t.gameObject;
                }
                // Match Ears1..6
                else if (name.StartsWith("Ears", StringComparison.OrdinalIgnoreCase) &&
                         int.TryParse(name.Substring("Ears".Length), out int earSlot))
                {
                    _accessoryMap[(FoodyItemType.Ears, earSlot)] = t.gameObject;
                }
                // Match Glasses1..6
                else if (name.StartsWith("Glasses", StringComparison.OrdinalIgnoreCase) &&
                         int.TryParse(name.Substring("Glasses".Length), out int glassSlot))
                {
                    _accessoryMap[(FoodyItemType.Glasses, glassSlot)] = t.gameObject;
                }
            }
        }

        public void EquipItem(string type, int slot)
        {
            EnsureAccessoriesCached();
            string normType = FoodyItemType.Normalize(type);
            UnequipItem(normType);

            if (_accessoryMap.TryGetValue((normType, slot), out var go))
            {
                go.SetActive(true);
            }
        }

        public void UnequipItem(string type)
        {
            EnsureAccessoriesCached();
            string normType = FoodyItemType.Normalize(type);
            for (int slot = 1; slot <= 6; slot++)
            {
                if (_accessoryMap.TryGetValue((normType, slot), out var go))
                {
                    go.SetActive(false);
                }
            }
        }

        public void UnequipAll()
        {
            EnsureAccessoriesCached();
            foreach (var kvp in _accessoryMap)
            {
                kvp.Value.SetActive(false);
            }
        }

        public bool IsEquipped(string type, int slot)
        {
            EnsureAccessoriesCached();
            string normType = FoodyItemType.Normalize(type);
            return _accessoryMap.TryGetValue((normType, slot), out var go) && go.activeSelf;
        }

        public int GetEquippedSlot(string type)
        {
            EnsureAccessoriesCached();
            string normType = FoodyItemType.Normalize(type);
            for (int slot = 1; slot <= 6; slot++)
            {
                if (_accessoryMap.TryGetValue((normType, slot), out var go) && go.activeSelf)
                {
                    return slot;
                }
            }
            return 0;
        }

        public void ApplyLoadout(FoodyLoadout loadout)
        {
            UnequipAll();
            if (loadout == null) return;

            if (loadout.antennas != null && loadout.antennas.slot > 0)
            {
                EquipItem(FoodyItemType.Antennas, loadout.antennas.slot);
            }
            if (loadout.ears != null && loadout.ears.slot > 0)
            {
                EquipItem(FoodyItemType.Ears, loadout.ears.slot);
            }
            if (loadout.glasses != null && loadout.glasses.slot > 0)
            {
                EquipItem(FoodyItemType.Glasses, loadout.glasses.slot);
            }
        }
    }
}
