using System;
using System.Text.RegularExpressions;
using Highlight.Engines;
using Highlight.Patterns;
using Highlight.UnityProxy;
using UnityEngine;

namespace BrewedInk.MarkdownSupport
{
    /// <summary>
    /// The RichTextEngine is an implementation of an Engine, from the <see cref="HighlightUtil"/> library.
    /// <b>This is not meant to be used, but has been left public for visibility and extension </b>
    /// </summary>
    public class RichTextEngine : Engine
    {
        protected override string ProcessBlockPatternMatch(Definition definition, BlockPattern pattern, Match match)
        {
            return Apply(pattern, match);
        }

        protected override string ProcessMarkupPatternMatch(Definition definition, MarkupPattern pattern, Match match)
        {
            return Apply(pattern, match);
        }

        protected override string ProcessWordPatternMatch(Definition definition, WordPattern pattern, Match match)
        {
            return Apply(pattern, match);
        }

        string Apply(Pattern pattern, Match match)
        {
            var foreground = ((UnityColor)pattern.Style.Colors.ForeColorProxy).color;

            foreground = Color.Lerp(foreground, Color.white, .5f);

            var foregroundStr = ColorUtility.ToHtmlStringRGBA(foreground);
            return $"<color=#{foregroundStr}>{match.Value}</color>";
        }
    }

    [Serializable]
    public class UnityColor : ColorProxy
    {
        public Color color;

        public static UnityColor FromColor(Color color) => new UnityColor
        {
            color = color
        };
    }
}