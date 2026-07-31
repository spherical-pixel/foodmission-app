using System;
using UnityEngine;

namespace BrewedInk.MarkdownSupport
{
    [HelpURL("https://github.com/cdhanna/MarkdownSupportDocs")]
    [Serializable]
    public class MarkdownAsset : ScriptableObject
    {
        public string text;

        public MarkdownVisualElement BuildRuntimeVisualElement() => BuildVisualElement(UMarkdownContext.Runtime());

        public MarkdownVisualElement BuildVisualElement(UMarkdownContext context)
        {
            return UMarkdown.Parse(text, context);
        }
    }
}