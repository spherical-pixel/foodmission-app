using System.Collections.Generic;
using UnityEngine;

namespace eu.foodmission.platform
{
    [System.Serializable]
    public class AvatarController : MonoBehaviour
    {
        

        [Header("Colores Predefinidos")]
        private List<Color> clothesColors = new List<Color>();
        private List<Color> hairColors = new List<Color>();
        private List<Color> skinColors = new List<Color>();
        private List<Color> eyesColors = new List<Color>();

        public IReadOnlyList<Color> ClothesColors => clothesColors;
        public IReadOnlyList<Color> HairColors => hairColors;
        public IReadOnlyList<Color> SkinColors => skinColors;
        public IReadOnlyList<Color> EyesColors => eyesColors;

        [Header("Partes del Avatar (Objetos)")]
        public List<GameObject> hairParts;
        public List<GameObject> noseParts;
        public GameObject eyebrowLeftGameObject;
        public GameObject eyebrowRightGameObject;
        public GameObject facialHairGameObject; // Para activar/desactivar toda la barba
        

        [Header("Materiales")]
        public Material hairMaterial;
        public Material eyebrowMaterial;
        public Material eyesMaterial;
        public Material skinMaterial;
        public Material mouthMaterial;
        public Material facialHairMaterial;
        public Material tshirtMaterial;
        public Material trouserMaterial;
        public Material shoesMaterial;

        [Header("Texturas")]
        public List<Texture> eyebrowTextures = new List<Texture>();
        public List<Texture> eyeTextures = new List<Texture>();
        public List<Texture> mouthTextures = new List<Texture>();
        public List<Texture> facialHairTextures = new List<Texture>();

        [Header("Mock Config")]
        public AvatarConfig mockConfig;

        [Header("Cameras")]
        public Camera avatarCamera;
        public Camera fullBodyCamera;

        [Header("Animator")]
        public Animator animator;
        private bool _colorsInitialized = false;

        private void Awake()
        {
            InitializeColors();
        }

#if UNITY_EDITOR
        [ContextMenu("Apply Mock Config")]
        private void ApplyMockConfigInEditor()
        {
            InitializeColors();
            ValidateIds(mockConfig);
            ApplyAvatar(mockConfig);
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif

        private void InitializeColors()
        {
            if( _colorsInitialized) return;
            

            clothesColors.Clear();
            hairColors.Clear();
            skinColors.Clear();
            eyesColors.Clear();

            // Clothes Colors

            string[] clothesHex = { "#FFFFFF", "#66DAFF", "#F11A39", "#578728", "#161616", "#FFD300", "#5C47CA", "#99FFB2", "#FFA0EE", "#543C12" };
            foreach (var hex in clothesHex) if (ColorUtility.TryParseHtmlString(hex, out Color c)) clothesColors.Add(c);

            // Hair Colors
            string[] hairHex = { "#FFFFFF", "#FFFE9B", "#FFE053", "#AB8D5E", "#9A7545", "#90514D", "#951A1A", "#46261D", "#1B2B54", "#161616" };
            foreach (var hex in hairHex) if (ColorUtility.TryParseHtmlString(hex, out Color c)) hairColors.Add(c);

            // Skin Colors
            string[] skinHex = { "#FFF3B6", "#FFDFB6", "#E2BF92", "#E9C8B3", "#C8AFA1", "#B79B76", "#8C795F", "#594934", "#462C2C", "#261B1E" };
            foreach (var hex in skinHex) if (ColorUtility.TryParseHtmlString(hex, out Color c)) skinColors.Add(c);

            // Eyes Colors
            string[] eyesHex = { "#3E2723", "#63472B", "#A0785A", "#495E35", "#2E5334", "#4682B4", "#82A1B1", "#607D8B", "#27445C", "#9E9E9E" };
            foreach (var hex in eyesHex) if (ColorUtility.TryParseHtmlString(hex, out Color c)) eyesColors.Add(c);

            _colorsInitialized = true;
        }

        private void ValidateIds(AvatarConfig avatarConfig)
        {
            avatarConfig.hair.idPart = Mathf.Clamp(avatarConfig.hair.idPart, 0, 10);
            avatarConfig.hair.idColor = Mathf.Clamp(avatarConfig.hair.idColor, 1, 10);
            avatarConfig.eyebrows.idPart = Mathf.Clamp(avatarConfig.eyebrows.idPart, 0, 10);
            avatarConfig.eyebrows.idColor = Mathf.Clamp(avatarConfig.eyebrows.idColor, 1, 10);
            avatarConfig.eyes.idPart = Mathf.Clamp(avatarConfig.eyes.idPart, 1, 10);
            avatarConfig.eyes.idColor = Mathf.Clamp(avatarConfig.eyes.idColor, 1, 10);
            avatarConfig.nose.idPart = Mathf.Clamp(avatarConfig.nose.idPart, 1, 10);
            avatarConfig.nose.idColor = Mathf.Clamp(avatarConfig.nose.idColor, 1, 10);
            avatarConfig.mouth.idPart = Mathf.Clamp(avatarConfig.mouth.idPart, 1, 10);
            avatarConfig.mouth.idColor = Mathf.Clamp(avatarConfig.mouth.idColor, 1, 10);
            avatarConfig.facialHair.idPart = Mathf.Clamp(avatarConfig.facialHair.idPart, 0, 5);
            avatarConfig.facialHair.idColor = Mathf.Clamp(avatarConfig.facialHair.idColor, 1, 10);
            avatarConfig.skin.idPart = Mathf.Clamp(avatarConfig.skin.idPart, 1, 10);
            avatarConfig.skin.idColor = Mathf.Clamp(avatarConfig.skin.idColor, 1, 10);
            avatarConfig.tshirt.idPart = Mathf.Clamp(avatarConfig.tshirt.idPart, 1, 10);
            avatarConfig.tshirt.idColor = Mathf.Clamp(avatarConfig.tshirt.idColor, 1, 10);
            avatarConfig.trousers.idPart = Mathf.Clamp(avatarConfig.trousers.idPart, 1, 10);
            avatarConfig.trousers.idColor = Mathf.Clamp(avatarConfig.trousers.idColor, 1, 10);
            avatarConfig.shoes.idPart = Mathf.Clamp(avatarConfig.shoes.idPart, 1, 10);
            avatarConfig.shoes.idColor = Mathf.Clamp(avatarConfig.shoes.idColor, 1, 10);
        }

        // Helper for Clamp if Math.Clamp is not available in older Unity versions
        private int Clamp(int value, int min, int max) => Mathf.Clamp(value, min, max);

        public void ApplyHair(AvatarPartConfig hairConfig)
        {
            if (hairConfig.idPart == 0)
            {
                foreach (var p in hairParts) if (p != null) p.SetActive(false);
            }
            else
            {
                for (int i = 0; i < hairParts.Count; i++)
                {
                    if (hairParts[i] != null) hairParts[i].SetActive(i == hairConfig.idPart - 1);
                }

                if (hairMaterial != null && hairConfig.idColor - 1 < hairColors.Count)
                {
                    hairMaterial.color = hairColors[hairConfig.idColor - 1];
                }
            }
        }

        public void ApplyEyebrows(AvatarPartConfig eyebrowsConfig)
        {
            if (eyebrowsConfig.idPart == 0)
            {
                if (eyebrowLeftGameObject != null) eyebrowLeftGameObject.SetActive(false);
                if (eyebrowRightGameObject != null) eyebrowRightGameObject.SetActive(false);
            }
            else
            {
                if (eyebrowRightGameObject != null) eyebrowRightGameObject.SetActive(true);
                if (eyebrowLeftGameObject != null) eyebrowLeftGameObject.SetActive(true);

                if (eyebrowMaterial != null)
                {
                    int idColor = eyebrowsConfig.idColor - 1;
                    if (idColor < hairColors.Count)
                    {
                        eyebrowMaterial.color = hairColors[idColor];
                    }
                    int idPart = eyebrowsConfig.idPart - 1;

                    if (idPart >= 0 && idPart < eyebrowTextures.Count)
                    {
                        eyebrowMaterial.mainTexture = eyebrowTextures[idPart];
                    }
                }
            }
        }

        public void ApplyEyes(AvatarPartConfig eyesConfig)
        {
            if (eyesMaterial != null)
            {
                int idColor = eyesConfig.idColor - 1;
                if (idColor < eyesColors.Count)
                {
                    eyesMaterial.color = eyesColors[idColor];
                }
                
                int idPart = eyesConfig.idPart - 1;
                if (idPart >= 0 && idPart < eyeTextures.Count)
                {
                    eyesMaterial.mainTexture = eyeTextures[idPart];
                }
            }
        }

        public void ApplyNose(AvatarPartConfig noseConfig)
        {
            int idPart = noseConfig.idPart - 1;
            int idColor = noseConfig.idColor - 1;
            if (idPart >= 1 && idPart <= noseParts.Count)
            {
                for (int i = 0; i < noseParts.Count; i++)
                {
                    if (noseParts[i] != null){
                        noseParts[i].SetActive(i == idPart);  
                    } 
                    else{
                        noseParts[i].SetActive(false);
                    }
                }
            
                if (skinMaterial != null && idColor < skinColors.Count)
                {
                    skinMaterial.color = skinColors[idColor];
                }
            }
        }

        public void ApplyMouth(AvatarPartConfig mouthConfig)
        {
            int idPart = mouthConfig.idPart - 1;
            if (idPart >= 1 && idPart <= mouthTextures.Count)
            {
                if (mouthMaterial != null)
                {
                    mouthMaterial.mainTexture = mouthTextures[idPart];
                }
            }
        }

        public void ApplyFacialHair(AvatarPartConfig facialHairConfig)
        {
            if (facialHairConfig.idPart == 0)
            {
                if (facialHairGameObject != null) facialHairGameObject.SetActive(false);
            }
            else
            {
                
                
                if (facialHairMaterial != null)
                {
                    if (facialHairGameObject != null) facialHairGameObject.SetActive(true);
                    int idColor = facialHairConfig.idColor - 1;
                    if (idColor < hairColors.Count)
                    {
                        facialHairMaterial.color = hairColors[idColor];
                    }
                    
                    int idPart = facialHairConfig.idPart - 1;
                    if (idPart >= 0 && idPart < facialHairTextures.Count)
                    {
                        facialHairMaterial.mainTexture = facialHairTextures[idPart];
                    }
                }
            }
        }

        public void ApplySkin(AvatarPartConfig skinConfig)
        {
            int idColor = skinConfig.idColor - 1;
            if (skinMaterial != null && idColor < skinColors.Count)
            {
                skinMaterial.color = skinColors[idColor];
            }
        }

        public void ApplyTshirt(AvatarPartConfig tshirtConfig)
        {
            int idColor = tshirtConfig.idColor - 1;
            if (tshirtMaterial != null && idColor < clothesColors.Count)
            {
                tshirtMaterial.color = clothesColors[idColor];                
            }
        }

        public void ApplyTrousers(AvatarPartConfig trousersConfig)
        {
            int idColor = trousersConfig.idColor - 1;
            if (trouserMaterial != null && idColor < clothesColors.Count)
                trouserMaterial.color = clothesColors[idColor];
        }

        public void ApplyShoes(AvatarPartConfig shoesConfig)
        {
            int idColor = shoesConfig.idColor - 1;
            if (shoesMaterial != null && idColor < clothesColors.Count)
                shoesMaterial.color = clothesColors[idColor];
        }

        public void ApplyAvatar(AvatarConfig avatarConfig)
        {
            ValidateIds(avatarConfig);
            ApplyHair(avatarConfig.hair);
            ApplyEyebrows(avatarConfig.eyebrows);
            ApplyEyes(avatarConfig.eyes);
            ApplyNose(avatarConfig.nose);
            ApplyMouth(avatarConfig.mouth);
            ApplyFacialHair(avatarConfig.facialHair);
            ApplySkin(avatarConfig.skin);
            ApplyTshirt(avatarConfig.tshirt);
            ApplyTrousers(avatarConfig.trousers);
            ApplyShoes(avatarConfig.shoes);
        }
    }
}
