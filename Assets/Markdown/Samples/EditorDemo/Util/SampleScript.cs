using BrewedInk.MarkdownSupport.Markdown.Samples.EditorDemo.Util;
using UnityEngine;

namespace BrewedInk.MarkdownSupport.Markdown.Samples.EditorDemo
{
    public class SampleScript : MonoBehaviour
    {
        [MarkdownSnippet(RelativeMarkdownFile = "../MarkdownDocs/EditorSample_fieldDoc.markdown")]
        public int x;

        [MarkdownSnippet(InlineMarkdown = "---\n A _number_ that will be printed to the console, `" + nameof(x) + "` times")]
        public int y;

        void Start()
        {
            for (var i = 0; i < x; i++)
            {
                Debug.Log(y);
            }
        }
    }
}