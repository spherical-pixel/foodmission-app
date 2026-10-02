using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

using NUnit.Framework;

using UnityEngine;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class ProgressWheelThemeContrastTests
    {
        private static readonly string[] k_RingVars = { "--fm-wheel-co2", "--fm-wheel-energy", "--fm-wheel-water", "--fm-wheel-land", "--fm-wheel-default" };

        [TestCase(".appui--light")]
        [TestCase(".appui--dark")]
        public void RingColours_HaveAtLeast3To1ContrastWithCard(string themeSelector)
        {
            string block = ThemeBlock(themeSelector);
            Color card = ParseColor(Var(block, "--fm-card-bg"));
            // The card is semi-transparent: check against the card composited over black and over white.
            Color overBlack = Composite(card, Color.black);
            Color overWhite = Composite(card, Color.white);

            foreach (string name in k_RingVars)
            {
                Color ring = ParseColor(Var(block, name));
                double worst = Math.Min(Contrast(ring, overBlack), Contrast(ring, overWhite));
                Assert.GreaterOrEqual(worst, 3.0, $"{themeSelector} {name} contrast {worst:F2}");
            }
        }

        private static string ThemeBlock(string selector)
        {
            string uss = File.ReadAllText(Path.Combine(Application.dataPath, "Foodmission/AppUI/Foodmission_Theme.uss"));
            int start = uss.IndexOf(selector + " {", StringComparison.Ordinal);
            Assert.GreaterOrEqual(start, 0, $"{selector} block not found");
            int end = uss.IndexOf("\n}", start, StringComparison.Ordinal);
            return uss.Substring(start, end - start);
        }

        private static string Var(string block, string name)
        {
            Match m = Regex.Match(block, Regex.Escape(name) + @"\s*:\s*([^;]+);");
            Assert.IsTrue(m.Success, $"{name} not defined");
            return m.Groups[1].Value.Trim();
        }

        private static Color ParseColor(string value)
        {
            if (value.StartsWith("#", StringComparison.Ordinal))
            {
                Assert.IsTrue(ColorUtility.TryParseHtmlString(value, out Color c), value);
                return c;
            }
            Match m = Regex.Match(value, @"rgba?\(\s*([\d.]+)\s*,\s*([\d.]+)\s*,\s*([\d.]+)\s*(?:,\s*([\d.]+))?\s*\)");
            Assert.IsTrue(m.Success, $"Unsupported colour {value}");
            float a = m.Groups[4].Success ? float.Parse(m.Groups[4].Value, CultureInfo.InvariantCulture) : 1f;
            return new Color(
                float.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture) / 255f,
                float.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture) / 255f,
                float.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture) / 255f, a);
        }

        private static Color Composite(Color top, Color bottom)
        {
            return new Color(
                top.r * top.a + bottom.r * (1 - top.a),
                top.g * top.a + bottom.g * (1 - top.a),
                top.b * top.a + bottom.b * (1 - top.a), 1f);
        }

        private static double Luminance(Color c)
        {
            double Channel(float v) => v <= 0.03928 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4);
            return 0.2126 * Channel(c.r) + 0.7152 * Channel(c.g) + 0.0722 * Channel(c.b);
        }

        private static double Contrast(Color a, Color b)
        {
            double la = Luminance(a), lb = Luminance(b);
            return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
        }
    }
}
