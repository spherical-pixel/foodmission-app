using System.IO;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

namespace BrewedInk.MarkdownSupport
{
    public class MarkdownTemplates
    {
        
        [MenuItem("Markdown File", menuItem = "Assets/Create/Markdown File", priority = 90)]
        [MenuItem("Markdown File", menuItem = "Assets/Create/"+ UMarkdownConstants.MENU_ROOT + "/Markdown File", priority = UMarkdownConstants.CREATE_CONFIG_ORDER - 1)]
        public static void CreateItemContent()
        {
            ProjectWindowUtil.CreateScriptAssetFromTemplateFile(GetTemplatePath(), $"MyMarkdownFile.markdown");
        }

        static string GetTemplatePath([CallerFilePath]string callerPath=null)
        {
            var localFile = Path.GetRelativePath(Application.dataPath, callerPath);
            var localPath = Path.GetDirectoryName(localFile);
            var path = Path.Combine("Assets", localPath, "ScriptTemplates", "markdown.markdown.txt");
            return path;
        }
    }
}