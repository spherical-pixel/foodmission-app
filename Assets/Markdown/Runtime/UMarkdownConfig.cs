using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace BrewedInk.MarkdownSupport
{
    [HelpURL("https://github.com/cdhanna/MarkdownSupportDocs")]
    [CreateAssetMenu(fileName=UMarkdownConstants.CONFIG_FILENAME, menuName = UMarkdownConstants.MENU_ROOT + "/Markdown Settings", order = UMarkdownConstants.CREATE_CONFIG_ORDER)]
    public class UMarkdownConfig : ScriptableObject
    {
        /// <inheritdoc cref="UMarkdownConfigData"/>
        public UMarkdownConfigData configData;

        private void Reset()
        {
            configData = GetDefault();
        }

        /// <summary>
        /// Get the system default <see cref="UMarkdownConfigData"/>
        /// </summary>
        /// <returns></returns>
        public static UMarkdownConfigData GetDefault()
        {
            return new UMarkdownConfigData
            {
                useCodeCopyButtons = true,
                validTextAssetExtensions = new string[] { "md" },
                styleSheets = new List<StyleSheet>
                {
                    Resources.Load<StyleSheet>(UMarkdownConstants.DEFAULT_STYLESHEET_FILENAME)
                }
            };
        }
    }
}