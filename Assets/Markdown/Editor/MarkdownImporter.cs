
using System.IO;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace BrewedInk.MarkdownSupport
{
    [HelpURL("https://github.com/cdhanna/MarkdownSupportDocs")]
    [ScriptedImporter(1, ".markdown", AllowCaching = false)]
    public class MarkdownImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var asset = ScriptableObject.CreateInstance<MarkdownAsset>();
            asset.text = File.ReadAllText(assetPath);
            ctx.AddObjectToAsset("text", asset);
            ctx.SetMainObject(asset);
        }
    }


}