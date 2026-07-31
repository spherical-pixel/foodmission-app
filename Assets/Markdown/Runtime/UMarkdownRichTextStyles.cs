using System;
using UnityEngine;

namespace BrewedInk.MarkdownSupport
{
    [HelpURL("https://github.com/cdhanna/MarkdownSupportDocs")]
    [CreateAssetMenu(menuName = UMarkdownConstants.MENU_ROOT + "/Rich Text Style", fileName = "RichTextStyle", order = UMarkdownConstants.CREATE_CONFIG_ORDER)]
    public class UMarkdownRichTextStyles : ScriptableObject
    {
        /// <inheritdoc cref="UMarkdownRichTextStyleData"/>
        public UMarkdownRichTextStyleData data = new UMarkdownRichTextStyleData();

        private void Reset()
        {
            data = GetDefault();
        }

        /// <summary>
        /// Get the system default <see cref="UMarkdownRichTextStyleData"/> data.
        /// </summary>
        /// <returns></returns>
        public static UMarkdownRichTextStyleData GetDefault()
        {
            return new UMarkdownRichTextStyleData
            {
                codeSelectionColor = new Color(1, 1, 1, .35f ),
                codeMarkupColor = new Color(.4f, .4f, .4f, .4f),
                codeFontAsset = "RobotoMono-Regular SDF"
            };
        }
    }

    /// <summary>
    /// The <see cref="UMarkdownRichTextStyleData"/> controls some styling of the compiled markdown
    /// that is not possible to set from USS, and is set from C# instead during the conversion of the
    /// markdown.
    /// </summary>
    [Serializable]
    public struct UMarkdownRichTextStyleData
    {
        /// <summary>
        /// When text in a code fence is selected, what color should the highlight be?
        /// </summary>
        public Color codeSelectionColor;
        
        /// <summary>
        /// When text is emphasised with code, like `this`, what color should the
        /// backdrop be?
        /// </summary>
        public Color codeMarkupColor;
        
        /// <summary>
        /// The name of a TextMeshPro font asset to use. Be aware that font assets
        /// must be placed in a "Resources/Font & Materials" folder (or whatever the given
        /// folder is in the TMP Settings).
        /// </summary>
        public string codeFontAsset;
    }
}