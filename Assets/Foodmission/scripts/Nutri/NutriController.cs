using System;
using System.Collections.Generic;
using UnityEngine;

namespace eu.foodmission.platform
{
    public class NutriController : MonoBehaviour
    {
        [Header("Animación y Cámara")]
        [SerializeField] private NutriAnimationController _nutriAnimationController;
        public NutriAnimationController NutriAnimationController => _nutriAnimationController;

        [SerializeField] private Camera _nutriCamera;
        public Camera NutriCamera => _nutriCamera;

        [Header("Accesorios")]
        [Tooltip("Ranuras 1..6 de Antenas (Elemento 0 = Ranura 1)")]
        [SerializeField] private List<GameObject> _antennas = new();

        [Tooltip("Ranuras 1..6 de Orejas (Elemento 0 = Ranura 1)")]
        [SerializeField] private List<GameObject> _ears = new();

        [Tooltip("Ranuras 1..6 de Gafas (Elemento 0 = Ranura 1)")]
        [SerializeField] private List<GameObject> _glasses = new();

        public IReadOnlyList<GameObject> Antennas => _antennas;
        public IReadOnlyList<GameObject> Ears => _ears;
        public IReadOnlyList<GameObject> Glasses => _glasses;

        private List<GameObject> GetListForType(string type)
        {
            string norm = FoodyItemType.Normalize(type);
            if (norm == FoodyItemType.Antennas) return _antennas;
            if (norm == FoodyItemType.Ears) return _ears;
            if (norm == FoodyItemType.Glasses) return _glasses;
            return null;
        }

        public void EquipItem(string type, int slot)
        {
            UnequipItem(type);

            var list = GetListForType(type);
            if (list == null || slot < 1 || slot > list.Count) return;

            var target = list[slot - 1];
            if (target != null)
            {
                target.SetActive(true);
            }
        }

        public void UnequipItem(string type)
        {
            var list = GetListForType(type);
            if (list == null) return;

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null)
                {
                    list[i].SetActive(false);
                }
            }
        }

        public void UnequipAll()
        {
            UnequipItem(FoodyItemType.Antennas);
            UnequipItem(FoodyItemType.Ears);
            UnequipItem(FoodyItemType.Glasses);
        }

        public bool IsEquipped(string type, int slot)
        {
            var list = GetListForType(type);
            if (list == null || slot < 1 || slot > list.Count) return false;

            var target = list[slot - 1];
            return target != null && target.activeSelf;
        }

        public int GetEquippedSlot(string type)
        {
            var list = GetListForType(type);
            if (list == null) return 0;

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null && list[i].activeSelf)
                {
                    return i + 1;
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
