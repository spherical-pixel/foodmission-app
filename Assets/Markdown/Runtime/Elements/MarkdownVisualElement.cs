using UnityEngine.UIElements;

namespace BrewedInk.MarkdownSupport
{
    /// <summary>
    /// The MarkdownVisualElement is a subclass of <see cref="VisualElement"/>, which is the
    /// base class for custom elements in Unity's UIToolkit.
    /// <para>
    /// This class is the root of rendered markdown.
    /// </para>
    /// </summary>
    public class MarkdownVisualElement : VisualElement
    {
        /// <inheritdoc cref="QAttribute{T}"/>
        public VisualElement QAttribute(string attributeName, string value) =>
            QAttribute<VisualElement>(attributeName, value);

        /// <summary>
        /// <b> WARNING: THIS METHOD USES REFLECTION! </b> <para></para>

        /// Find the first instance of the VisualElement, <typeparamref name="T"/>, that has
        /// a UXML attribute of <paramref name="attributeName"/> equal to <paramref name="value"/>.
        ///
        /// <para>
        /// Attributes can be set on markdown elements using the extended markdown attribute syntax. 
        /// </para>
        /// 
        /// <para>
        /// This method is meant as light syntax sugar, and internally uses the <see cref="QueryBuilderExtensions.HasAttribute"/>
        /// method.
        /// </para>
        /// </summary>
        /// <param name="attributeName">
        /// The name of the attribute being used as a filter. 
        /// </param>
        /// <param name="value">
        /// The exact value match
        /// </param>
        /// <typeparam name="T">
        /// The type of VisualElement being used as a filter
        /// </typeparam>
        /// <returns>The first instance of a VisualElement that matches the search.</returns>
        public T QAttribute<T>(string attributeName, string value)
            where T : VisualElement
        {
            var builder = this.Query<T>()
                .HasAttribute(attributeName, x => x == value);
            return builder.Build().First();
        }

        /// <summary>
        /// This method will ensure that the given element is currently visible in the
        /// MarkdownVisualElement's view.
        /// <para>
        /// When the markdown document is larger (vertically) than the containing element, there
        /// should be a <see cref="ScrollView"/> element somewhere in the parent lineage.
        /// This method will find that scroll view, and use its own <see cref="ScrollView.ScrollTo(VisualElement)"/>
        /// function. 
        /// </para>
        /// <para>
        /// <b>Important!</b> If there is no <see cref="ScrollView"/> in the parent lineage, this function
        /// is essentially a no-op, and does nothing.
        /// </para>
        /// <para>
        /// <b>Also Important!</b> The Unity Editor will usually insert scroll views automatically, but
        /// the Runtime will not. You are responsible for making sure there is a <see cref="ScrollView"/>
        /// in the parent lineage when running this in Runtime.
        /// </para>
        /// </summary>
        /// <param name="anchor">
        /// A string starting with <i>#</i>, representing the id of an element to scroll to.
        /// In traditional markdown, any heading is an anchor with an id equal to the content of the
        /// header, but with spaces converted to dashes, and all lowercase.
        /// <para>
        /// For example, the markdown below would have an anchor, <i>"#hello-world"</i>
        /// <para>
        /// <code>
        /// # Hello World
        /// </code>
        /// </para>
        /// </para>
        /// </param>
        public void ScrollTo(string anchor)
        {
            var scroller = FindScroll();
            if (scroller == null) return; // if there is no scroll, everything must be in view. Shrug?

            var element = this.Q<VisualElement>(anchor.Replace("#", ""));
            ScrollTo(element);
        }

        /// <summary>
        /// This method will ensure that the given element is currently visible in the
        /// MarkdownVisualElement's view.
        /// <para>
        /// When the markdown document is larger (vertically) than the containing element, there
        /// should be a <see cref="ScrollView"/> element somewhere in the parent lineage.
        /// This method will find that scroll view, and use its own <see cref="ScrollView.ScrollTo(VisualElement)"/>
        /// function. 
        /// </para>
        /// <para>
        /// <b>Important!</b> If there is no <see cref="ScrollView"/> in the parent lineage, this function
        /// is essentially a no-op, and does nothing.
        /// </para>
        /// <para>
        /// <b>Also Important!</b> The Unity Editor will usually insert scroll views automatically, but
        /// the Runtime will not. You are responsible for making sure there is a <see cref="ScrollView"/>
        /// in the parent lineage when running this in Runtime.
        /// </para>
        /// </summary>
        /// <param name="subElement">
        /// The Element that needs to be visible. This element must exist as a child of the markdown document. If
        /// it doesn't, then this function is a no-op.
        /// </param>
        public void ScrollTo(VisualElement subElement)
        {
            if (subElement == null) return;
            var scroller = FindScroll();
            if (scroller == null) return; // if there is no scroll, everything must be in view. Shrug?
            
            scroller.ScrollTo(subElement);
        }
        
        ScrollView FindScroll()
        {
            var candidate = parent;
            while (candidate != null)
            {
                if (candidate is ScrollView root)
                    return root;
                candidate = candidate.parent;
            }
            return null;
        }

    }
}