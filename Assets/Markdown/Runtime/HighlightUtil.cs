using System.Collections.Generic;
using Highlight.Configuration;
using Highlight.UnityProxy;
using UnityEngine;

namespace BrewedInk.MarkdownSupport
{
    /// <summary>
    /// Highlight is an open-source tool for highlighting source code.
    /// MarkdownSupport forked a version of the Highlight library, https://github.com/thomasjo/highlight
    /// and modified it to work with netstandard, and to proxy the Color code to Unity's Color struct.
    /// Unfortunately, because the library is imported via .DLL, it is hard to extend or modify. 
    /// </summary>
    public static class HighlightUtil
    {
        private static IConfiguration _config;

        private static Dictionary<string, string> _strToCodeName;
        static HighlightUtil()
        {
            
            ColorUtil.Black = UnityColor.FromColor(Color.black);
            ColorUtil.Clear = UnityColor.FromColor(Color.clear);
            ColorUtil.FromCss = str =>
            {
                if (ColorUtility.TryParseHtmlString(str, out var color))
                {
                    return UnityColor.FromColor(color);
                }
                else
                {
                    return UnityColor.FromColor(Color.black);
                }
            };
            _config = new DefaultConfiguration();
            _strToCodeName = new Dictionary<string, string>();
            foreach (var kvp in _config.Definitions)
            {
                _strToCodeName[kvp.Key] = kvp.Key;
                _strToCodeName[kvp.Key.ToLowerInvariant()] = kvp.Key;
            }
            
            _strToCodeName["csharp"] = "C#";
            _strToCodeName["js"] = "JavaScript";
        }
        
        public static string CodeLangToName(string codeLang)
        {
            if (!_strToCodeName.TryGetValue(codeLang, out var name))
            {
                name = "";
            }

            return name;
        }
    }
}