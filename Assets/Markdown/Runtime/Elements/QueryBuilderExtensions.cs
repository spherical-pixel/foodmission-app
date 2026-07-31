using System;
using UnityEngine.UIElements;

namespace BrewedInk.MarkdownSupport
{
    public static class QueryBuilderExtensions
    {
        /// <summary>
        /// <b> WARNING: THIS METHOD USES REFLECTION! </b> <para></para>
        /// Appends a predicate filter to a <see cref="UQueryBuilder{T}"/> that will use
        /// Reflection to find an attribute and check its value against the given filter function.
        /// </summary>
        /// <param name="self"></param>
        /// <param name="attributeName"></param>
        /// <param name="attributePredicate"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static UQueryBuilder<T> HasAttribute<T>(this UQueryBuilder<T> self, string attributeName, Func<string, bool> attributePredicate)
            where T : VisualElement
        {
            return self.Where(element =>
                element.TryGetMarkdownProperty(attributeName, out var instanceValue) &&
                attributePredicate(instanceValue));
        }
    }
}