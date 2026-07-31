using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

namespace BrewedInk.MarkdownSupport
{
    /// <summary>
    /// This class uses Reflection to get access to VisualElement properties (commonly referred to as Attributes)
    /// </summary>
    public static class VisualElementExtensions
    {
        private static MethodInfo _getPropMethod;
        private static MethodInfo _setPropMethod;

        static VisualElementExtensions()
        {
            var type = typeof(VisualElement);
            _getPropMethod = type.GetMethod("GetProperty", BindingFlags.Instance | BindingFlags.NonPublic);
            _setPropMethod = type.GetMethod("SetProperty", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        /// <remarks>
        /// This currently doesn't do anything, but the intention is that the attribute name may be different
        /// than the UXML property name. 
        /// </remarks>
        public static string GetMarkdownAttributeName(string id) => id;
        
        /// <summary>
        /// <b> WARNING: THIS METHOD USES REFLECTION! </b> <para></para>
        /// UXML is similar to HTML, and HTML elements can have attributes, like in the code below,
        /// <code>
        /// 
        /// &lt;div attr="hello"/&gt;
        /// </code>
        ///
        /// When creating a VisualElement from source code, Unity allows for attributes like this, but
        /// (1) calls them properties, and (2), marks them as private.
        /// <para>
        /// You can access those attributes using this method.
        /// </para>
        /// <para>
        /// Attribute names should follow the standard naming convention and use hyphens instead of spaces or underscores.
        /// </para>
        /// <para>
        /// Attribute values are always strings. Null is not valid.
        /// </para>
        /// <para>
        /// See the <see cref="SetMarkdownProperty"/> method to set an attribute value.
        /// </para>
        /// </summary>
        /// <param name="element">The visual element to search for attributes on</param>
        /// <param name="markdownAttributeName">the name of the attribute</param>
        /// <param name="value">the out parameter where the value of the attribute is stored. </param>
        /// <returns>
        /// True if there is a non-null value for the attribute. False otherwise. 
        /// </returns>
        public static bool TryGetMarkdownProperty(this VisualElement element, string markdownAttributeName, out string value)
        {
            var objValue = _getPropMethod.Invoke(element,
                new object[] { new PropertyName(GetMarkdownAttributeName(markdownAttributeName)) });
            value = objValue as string;
            return value != null;
        }

        /// <summary>
        /// <b> WARNING: THIS METHOD USES REFLECTION! </b> <para></para>
        /// UXML is similar to HTML, and HTML elements can have attributes, like in the code below,
        /// <code>
        /// 
        /// &lt;div attr="hello"/&gt;
        /// </code>
        ///
        /// When creating a VisualElement from source code, Unity allows for attributes like this, but
        /// (1) calls them properties, and (2), marks them as private.
        /// <para>
        /// You can set those attributes using this method.
        /// </para>
        /// <para>
        /// Attribute names should follow the standard naming convention and use hyphens instead of spaces or underscores.
        /// </para>
        /// <para>
        /// Attribute values are always strings. Null is not valid.
        /// </para>
        /// <para>
        /// See the <see cref="TryGetMarkdownProperty"/> method to read an attribute value.
        /// </para>
        /// </summary>
        /// <param name="element">The visual element to set attributes on</param>
        /// <param name="markdownAttributeName">the name of the attribute</param>
        /// <param name="value">the new value of the attribute </param>
        public static void SetMarkdownProperty(this VisualElement element, string markdownAttributeName, string value)
        {
            _setPropMethod.Invoke(element, new object[]
            {
                new PropertyName(GetMarkdownAttributeName(markdownAttributeName)),
                value
            });
        }
    }
}