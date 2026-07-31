using UnityEngine.UIElements;

namespace BrewedInk.MarkdownSupport
{
    public class SelectableLabel : Label 
    {
        public SelectableLabel(string text) : base(text)
        {
            
#if UNITY_2022_1_OR_NEWER
            selection.isSelectable = true;
            focusable = true;
#endif
        }
        public SelectableLabel() : this("")
        {
        }

    }
}