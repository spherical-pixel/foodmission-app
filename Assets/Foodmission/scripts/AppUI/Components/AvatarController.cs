using System.Collections.Generic;
using UnityEngine;

namespace eu.foodmission.platform
{
    [System.Serializable]
    public class AvatarController : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");

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
        public GameObject facialHairGameObject;

        [Header("Renderers")]
        public Renderer headRenderer;
        public Renderer mouthRenderer;
        public Renderer eyebrowRightRenderer;
        public Renderer eyebrowLeftRenderer;
        public Renderer eyeRightRenderer;
        public Renderer eyeLeftRenderer;
        public Renderer beardRenderer;
        public Renderer handRightRenderer;
        public Renderer handLeftRenderer;
        public Renderer bodyRenderer;
        public Renderer legRightRenderer;
        public Renderer legLeftRenderer;
        public List<Renderer> noseRenderers;
        public List<Renderer> hairRenderers;

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
        private MaterialPropertyBlock _propertyBlock;

        private void Awake()
        {
            InitializeColors();
            _propertyBlock = new MaterialPropertyBlock();
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
            if (_colorsInitialized) return;

            clothesColors.Clear();
            hairColors.Clear();
            skinColors.Clear();
            eyesColors.Clear();

            string[] clothesHex = { "#FFFFFF", "#66DAFF", "#F11A39", "#578728", "#161616", "#FFD300", "#5C47CA", "#99FFB2", "#FFA0EE", "#543C12" };
            foreach (var hex in clothesHex) if (ColorUtility.TryParseHtmlString(hex, out Color c)) clothesColors.Add(c);

            string[] hairHex = { "#FFFFFF", "#FFFE9B", "#FFE053", "#AB8D5E", "#9A7545", "#90514D", "#951A1A", "#46261D", "#1B2B54", "#161616" };
            foreach (var hex in hairHex) if (ColorUtility.TryParseHtmlString(hex, out Color c)) hairColors.Add(c);

            string[] skinHex = { "#FFF3B6", "#FFDFB6", "#E2BF92", "#E9C8B3", "#C8AFA1", "#B79B76", "#8C795F", "#594934", "#462C2C", "#261B1E" };
            foreach (var hex in skinHex) if (ColorUtility.TryParseHtmlString(hex, out Color c)) skinColors.Add(c);

            string[] eyesHex = { "#3E2723", "#63472B", "#A0785A", "#495E35", "#2E5334", "#4682B4", "#82A1B1", "#607D8B", "#27445C", "#9E9E9E" };
            foreach (var hex in eyesHex) if (ColorUtility.TryParseHtmlString(hex, out Color c)) eyesColors.Add(c);

            _colorsInitialized = true;
        }

        private void ValidateIds(AvatarConfig avatarConfig)
        {
            if (avatarConfig == null) return;
            if (avatarConfig.hair != null)
            {
                avatarConfig.hair.idPart = Mathf.Clamp(avatarConfig.hair.idPart, 0, 10);
                avatarConfig.hair.idColor = Mathf.Clamp(avatarConfig.hair.idColor, 1, 10);
            }
            if (avatarConfig.eyebrows != null)
            {
                avatarConfig.eyebrows.idPart = Mathf.Clamp(avatarConfig.eyebrows.idPart, 0, 10);
                avatarConfig.eyebrows.idColor = Mathf.Clamp(avatarConfig.eyebrows.idColor, 1, 10);
            }
            if (avatarConfig.eyes != null)
            {
                avatarConfig.eyes.idPart = Mathf.Clamp(avatarConfig.eyes.idPart, 1, 10);
                avatarConfig.eyes.idColor = Mathf.Clamp(avatarConfig.eyes.idColor, 1, 10);
            }
            if (avatarConfig.nose != null)
            {
                avatarConfig.nose.idPart = Mathf.Clamp(avatarConfig.nose.idPart, 1, 10);
                avatarConfig.nose.idColor = Mathf.Clamp(avatarConfig.nose.idColor, 1, 10);
            }
            if (avatarConfig.mouth != null)
            {
                avatarConfig.mouth.idPart = Mathf.Clamp(avatarConfig.mouth.idPart, 1, 10);
                avatarConfig.mouth.idColor = Mathf.Clamp(avatarConfig.mouth.idColor, 1, 10);
            }
            if (avatarConfig.facialHair != null)
            {
                avatarConfig.facialHair.idPart = Mathf.Clamp(avatarConfig.facialHair.idPart, 0, 5);
                avatarConfig.facialHair.idColor = Mathf.Clamp(avatarConfig.facialHair.idColor, 1, 10);
            }
            if (avatarConfig.skin != null)
            {
                avatarConfig.skin.idPart = Mathf.Clamp(avatarConfig.skin.idPart, 1, 10);
                avatarConfig.skin.idColor = Mathf.Clamp(avatarConfig.skin.idColor, 1, 10);
            }
            if (avatarConfig.tshirt != null)
            {
                avatarConfig.tshirt.idPart = Mathf.Clamp(avatarConfig.tshirt.idPart, 1, 10);
                avatarConfig.tshirt.idColor = Mathf.Clamp(avatarConfig.tshirt.idColor, 1, 10);
            }
            if (avatarConfig.trousers != null)
            {
                avatarConfig.trousers.idPart = Mathf.Clamp(avatarConfig.trousers.idPart, 1, 10);
                avatarConfig.trousers.idColor = Mathf.Clamp(avatarConfig.trousers.idColor, 1, 10);
            }
            if (avatarConfig.shoes != null)
            {
                avatarConfig.shoes.idPart = Mathf.Clamp(avatarConfig.shoes.idPart, 1, 10);
                avatarConfig.shoes.idColor = Mathf.Clamp(avatarConfig.shoes.idColor, 1, 10);
            }
        }

        private int Clamp(int value, int min, int max) => Mathf.Clamp(value, min, max);

        public void ApplyHair(AvatarPartConfig hairConfig)
        {
            if (hairConfig == null) return;
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

                if (hairConfig.idColor - 1 < hairColors.Count)
                {
                    _propertyBlock.Clear();
                    _propertyBlock.SetColor(BaseColorId, hairColors[hairConfig.idColor - 1]);
                    foreach (var renderer in hairRenderers)
                    {
                        if (renderer != null)
                            renderer.SetPropertyBlock(_propertyBlock);
                    }
                }
            }
        }

        public void ApplyEyebrows(AvatarPartConfig eyebrowsConfig)
        {
            if (eyebrowsConfig == null) return;
            if (eyebrowsConfig.idPart == 0)
            {
                if (eyebrowLeftGameObject != null) eyebrowLeftGameObject.SetActive(false);
                if (eyebrowRightGameObject != null) eyebrowRightGameObject.SetActive(false);
            }
            else
            {
                if (eyebrowRightGameObject != null) eyebrowRightGameObject.SetActive(true);
                if (eyebrowLeftGameObject != null) eyebrowLeftGameObject.SetActive(true);

                _propertyBlock.Clear();
                int idColor = eyebrowsConfig.idColor - 1;
                if (idColor < hairColors.Count)
                {
                    _propertyBlock.SetColor(BaseColorId, hairColors[idColor]);
                }
                int idPart = eyebrowsConfig.idPart - 1;
                if (idPart >= 0 && idPart < eyebrowTextures.Count && eyebrowTextures[idPart] != null)
                {
                    _propertyBlock.SetTexture(BaseMapId, eyebrowTextures[idPart]);
                }

                if (eyebrowRightRenderer != null)
                    eyebrowRightRenderer.SetPropertyBlock(_propertyBlock);
                if (eyebrowLeftRenderer != null)
                    eyebrowLeftRenderer.SetPropertyBlock(_propertyBlock);
            }
        }

        public void ApplyEyes(AvatarPartConfig eyesConfig)
        {
            if (eyesConfig == null) return;
            _propertyBlock.Clear();
            int idColor = eyesConfig.idColor - 1;
            if (idColor < eyesColors.Count)
            {
                _propertyBlock.SetColor(BaseColorId, eyesColors[idColor]);
            }

            int idPart = eyesConfig.idPart - 1;
            if (idPart >= 0 && idPart < eyeTextures.Count && eyeTextures[idPart] != null)
            {
                _propertyBlock.SetTexture(BaseMapId, eyeTextures[idPart]);
            }

            if (eyeRightRenderer != null)
                eyeRightRenderer.SetPropertyBlock(_propertyBlock);
            if (eyeLeftRenderer != null)
                eyeLeftRenderer.SetPropertyBlock(_propertyBlock);
        }

        public void ApplyNose(AvatarPartConfig noseConfig)
        {
            if (noseConfig == null) return;
            int idPart = noseConfig.idPart - 1;
            if (idPart >= 1 && idPart <= noseParts.Count)
            {
                for (int i = 0; i < noseParts.Count; i++)
                {
                    if (noseParts[i] != null)
                    {
                        noseParts[i].SetActive(i == idPart);
                    }
                    else
                    {
                        if (i < noseRenderers.Count && noseRenderers[i] != null)
                            noseRenderers[i].gameObject.SetActive(false);
                    }
                }
            }
        }

        public void ApplyMouth(AvatarPartConfig mouthConfig)
        {
            if (mouthConfig == null) return;
            int idPart = mouthConfig.idPart - 1;
            if (idPart >= 1 && idPart < mouthTextures.Count && mouthRenderer != null && mouthTextures[idPart] != null)
            {
                _propertyBlock.Clear();
                _propertyBlock.SetTexture(BaseMapId, mouthTextures[idPart]);
                mouthRenderer.SetPropertyBlock(_propertyBlock);
            }
        }

        public void ApplyFacialHair(AvatarPartConfig facialHairConfig)
        {
            if (facialHairConfig == null) return;
            if (facialHairConfig.idPart == 0)
            {
                if (facialHairGameObject != null) facialHairGameObject.SetActive(false);
            }
            else
            {
                if (beardRenderer != null)
                {
                    if (facialHairGameObject != null) facialHairGameObject.SetActive(true);

                    _propertyBlock.Clear();
                    int idColor = facialHairConfig.idColor - 1;
                    if (idColor < hairColors.Count)
                    {
                        _propertyBlock.SetColor(BaseColorId, hairColors[idColor]);
                    }

                    int idPart = facialHairConfig.idPart - 1;
                    if (idPart >= 0 && idPart < facialHairTextures.Count && facialHairTextures[idPart] != null)
                    {
                        _propertyBlock.SetTexture(BaseMapId, facialHairTextures[idPart]);
                    }

                    beardRenderer.SetPropertyBlock(_propertyBlock);
                }
            }
        }

        public void ApplySkin(AvatarPartConfig skinConfig)
        {
            if (skinConfig == null) return;
            int idColor = skinConfig.idColor - 1;
            if (idColor < skinColors.Count)
            {
                _propertyBlock.Clear();
                _propertyBlock.SetColor(BaseColorId, skinColors[idColor]);

                if (headRenderer != null)
                    headRenderer.SetPropertyBlock(_propertyBlock);
                if (handRightRenderer != null)
                    handRightRenderer.SetPropertyBlock(_propertyBlock, 1);
                if (handLeftRenderer != null)
                    handLeftRenderer.SetPropertyBlock(_propertyBlock, 1);
                foreach (var nose in noseRenderers)
                {
                    if (nose != null)
                        nose.SetPropertyBlock(_propertyBlock);
                }
            }
        }

        public void ApplyTshirt(AvatarPartConfig tshirtConfig)
        {
            if (tshirtConfig == null) return;
            int idColor = tshirtConfig.idColor - 1;
            if (idColor < clothesColors.Count)
            {
                _propertyBlock.Clear();
                _propertyBlock.SetColor(BaseColorId, clothesColors[idColor]);

                if (bodyRenderer != null)
                    bodyRenderer.SetPropertyBlock(_propertyBlock, 0);
                if (handRightRenderer != null)
                    handRightRenderer.SetPropertyBlock(_propertyBlock, 0);
                if (handLeftRenderer != null)
                    handLeftRenderer.SetPropertyBlock(_propertyBlock, 0);
            }
        }

        public void ApplyTrousers(AvatarPartConfig trousersConfig)
        {
            if (trousersConfig == null) return;
            int idColor = trousersConfig.idColor - 1;
            if (idColor < clothesColors.Count)
            {
                _propertyBlock.Clear();
                _propertyBlock.SetColor(BaseColorId, clothesColors[idColor]);

                if (bodyRenderer != null)
                    bodyRenderer.SetPropertyBlock(_propertyBlock, 1);
                if (legRightRenderer != null)
                    legRightRenderer.SetPropertyBlock(_propertyBlock, 1);
                if (legLeftRenderer != null)
                    legLeftRenderer.SetPropertyBlock(_propertyBlock, 1);
            }
        }

        public void ApplyShoes(AvatarPartConfig shoesConfig)
        {
            if (shoesConfig == null) return;
            int idColor = shoesConfig.idColor - 1;
            if (idColor < clothesColors.Count)
            {
                _propertyBlock.Clear();
                _propertyBlock.SetColor(BaseColorId, clothesColors[idColor]);

                if (legRightRenderer != null)
                    legRightRenderer.SetPropertyBlock(_propertyBlock, 0);
                if (legLeftRenderer != null)
                    legLeftRenderer.SetPropertyBlock(_propertyBlock, 0);
            }
        }

        public void ApplyAvatar(AvatarConfig avatarConfig)
        {
            if (avatarConfig == null) return;
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
