using System;
using System.IO;
using System.Linq;
using UnityEngine;
using Object = System.Object;

namespace BrewedInk.MarkdownSupport
{
    [Serializable]
    public class MarkdownLinkUri
    {
        private string _link;
        private UMarkdownContext _context;

        public readonly MarkdownLinkType type;
        public readonly string qualifiedLink;
        public bool isFileTexture;

        private static readonly string[] ImageFileExtensions = new string[]
        {
            ".jpg", ".jpeg", ".png", ".bmp"
        };

        public MarkdownLinkUri(string link, UMarkdownContext context)
        {
            _context = context;
            _link = link;
            var lowerLink = _link.ToLowerInvariant();

            if (_link.StartsWith("#"))
            {
                type = MarkdownLinkType.Local;
                qualifiedLink = link;
            } else if (lowerLink.StartsWith("http://") || _link.StartsWith("https://"))
            {
                type = MarkdownLinkType.Web;
                qualifiedLink = link;
            }
            else
            {
                type = MarkdownLinkType.File;

                if (link.StartsWith("./"))
                {
                    link = link.Substring(2);
                }
                
                if (!Path.IsPathFullyQualified(link))
                {
                    qualifiedLink = Path.Combine(context.rootFilePath, link);
                }
                else
                {
                    qualifiedLink = link;
                }
                
            }

            if (type == MarkdownLinkType.File)
            {
                if (File.Exists(qualifiedLink) && ImageFileExtensions.Any(x => qualifiedLink.EndsWith(x)))
                {
                    isFileTexture = true;
                }
            }
        }

        public void Open(MarkdownVisualElement root)
        {
            var openUri = this;
            if (openUri.type == MarkdownLinkType.Local)
            {
                if (root == null)
                {
                    throw new Exception("Cannot navigate when Markdown is not in a MarkdownVisualElement");
                }
                root.ScrollTo(qualifiedLink);
            }
            else if (openUri.type == MarkdownLinkType.Web)
            {
                Application.OpenURL(qualifiedLink);
            } else if (openUri.type == MarkdownLinkType.File)
            {
                    
#if UNITY_EDITOR
                var fileUrl = openUri.qualifiedLink;
                if (Path.IsPathFullyQualified(fileUrl))
                {
                    fileUrl =  "Assets" + fileUrl.Substring(Application.dataPath.Length);
                }
                var maybeAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(fileUrl);
                if (maybeAsset != null)
                {
                    UnityEditor.Selection.SetActiveObjectWithContext(maybeAsset, null);
                    return;
                }
#endif
                    
                var fileUri = "file://" + Path.GetFullPath(openUri.qualifiedLink);
                Application.OpenURL(fileUri);
            }
        }
    }

    public enum MarkdownLinkType
    {
        Local,
        Web,
        File
    }
}