using System;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UIElements;

namespace BrewedInk.MarkdownSupport.Markdown.Samples.EditorDemo.Util
{
    [AttributeUsage(AttributeTargets.Field)]
    public class MarkdownSnippetAttribute : PropertyAttribute
    {
        public string InlineMarkdown { get; set; }
        public string RelativeMarkdownFile { get; set; }
        
        private readonly string _callerDir;

        public MarkdownSnippetAttribute([CallerFilePath]string callerPath=null)
        {
            // The [CallerFilePath] is a neat trick in C# that automatically inserts the filepath
            // of the C# source code that invokes the function. This is only usable for editor-time
            // applications, where you have all the source code.
            _callerDir = Path.GetDirectoryName(callerPath);
        }
        
        public VisualElement GetElement()
        {
            // if there is inline markdown, use that.
            if (!string.IsNullOrEmpty(InlineMarkdown))
            {
                return UMarkdown.Parse(
                    InlineMarkdown, 
                    UMarkdownContext.FromFile(
                        filePath: Application.dataPath, 
                        isRuntime: false));
            }

            // otherwise, load up the given markdown file!
            var path = Path.Combine(_callerDir, RelativeMarkdownFile);
            if (File.Exists(path))
            {
                var markdown = File.ReadAllText(path);
                return UMarkdown.Parse(
                    markdown, 
                    UMarkdownContext.FromFile(
                        filePath: path, 
                        isRuntime: false));
            }

            // if that didn't work, render an error...
            return new Label("no markdown at " + path);
        }
    }
}