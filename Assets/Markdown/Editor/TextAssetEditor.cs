using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace BrewedInk.MarkdownSupport
{
    [CustomEditor(typeof(TextAsset))]
    public class TextAssetEditor : Editor
    {
        VisualElement CreateDefault(TextAsset asset)
        {
            var text = asset.text.Substring(0, Math.Min(1000, asset.text.Length));
            var lbl = new SelectableLabel(text);
            return lbl;
        }
        
        public override VisualElement CreateInspectorGUI()
        {
            if (!(target is TextAsset asset)) return new Label("Not a text asset");
            
            var path = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(path)) return CreateDefault(asset);

            var config = UMarkdownConfigData.LoadProjectConfig();
            foreach (var ext in config.validTextAssetExtensions)
            {
                if (!path.EndsWith(ext)) continue;
                var context = UMarkdownContext.FromFile(path, false, config);
                var element = UMarkdown.Parse(asset.text, context);
                return element;
            }
            return CreateDefault(asset);
            
        }
    }
}