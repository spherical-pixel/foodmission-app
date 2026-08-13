using System;
using UnityEngine;

namespace eu.foodmission.platform
{
    public class NutriController : MonoBehaviour
    {
        [SerializeField] private NutriAnimationController _nutriAnimationController;

        public NutriAnimationController NutriAnimationController => _nutriAnimationController;

        [SerializeField] private Camera _nutriCamera;
        public Camera NutriCamera => _nutriCamera;
    }
}
