using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;
using UnityEngine.UIElements;

namespace BrewedInk.MarkdownSupport
{
    [CustomEditor(typeof(MarkdownImporter))]
    public class MarkdownImporterEditor : ScriptedImporterEditor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var container = new VisualElement();
            container.style.overflow = Overflow.Hidden;
            var applyButtons = new IMGUIContainer(this.ApplyRevertGUI);
            applyButtons.style.maxHeight = 0;
            applyButtons.style.right = -500; // dumb hack to get the buttons out of the way...
            applyButtons.style.position = Position.Absolute;
            
            container.Add(applyButtons);

            if (!(assetTarget is MarkdownAsset asset)) return container;
            if (!(target is ScriptedImporter importer)) return container;

            var context = UMarkdownContext.FromFile(importer.assetPath, false, UMarkdownConfigData.LoadProjectConfig());
            var content = UMarkdown.Parse(asset.text, context);
            
            container.Add(content);
            
            return container;
        }
        public override bool showImportedObject => false;
    }
}