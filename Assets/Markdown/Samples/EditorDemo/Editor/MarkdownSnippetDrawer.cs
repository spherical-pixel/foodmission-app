
using BrewedInk.MarkdownSupport.Markdown.Samples.EditorDemo.Util;
using UnityEditor;
using UnityEngine.UIElements;

namespace BrewedInk.MarkdownSupport.Markdown.Samples.EditorDemo.Editor
{
    [CustomPropertyDrawer(typeof(MarkdownSnippetAttribute))]
    public class MarkdownSnippetDrawer : DecoratorDrawer
    {
        public override VisualElement CreatePropertyGUI()
        {
            var markdownAttribute = attribute as MarkdownSnippetAttribute;
            var element = markdownAttribute.GetElement();
            
            // add some vertical spacing
            element.style.marginTop = 12;
            return element;
        }
    }
}