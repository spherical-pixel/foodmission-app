using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    /// <summary>
    /// UI Toolkit ignores a var() that is never defined (no warning): the property silently falls back
    /// to its default, e.g. a transparent background or the default text colour.
    /// </summary>
    [TestFixture]
    public class ThemeVariableTests
    {
        private const string ThemePath = "Assets/Foodmission/AppUI/Foodmission_Theme.uss";
        private const string AppUiPackage = "Packages/com.unity.dt.app-ui";

        private static readonly Regex k_Definition = new Regex(@"(--[A-Za-z0-9_\-]+)\s*:");
        private static readonly Regex k_Use = new Regex(@"var\(\s*(--[A-Za-z0-9_\-]+)\s*(,)?");
        private static readonly Regex k_Block = new Regex(@"([^{}]*)\{([^{}]*)\}");

        [Test]
        public void EveryVariableUsed_IsDefined()
        {
            var defined = new HashSet<string>(StringComparer.Ordinal);
            string appUi = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(AppUiPackage)?.resolvedPath;
            Assert.IsNotNull(appUi, $"{AppUiPackage} not found");
            IEnumerable<string> sheets = StyleSheets(StaticAssetScan.Root).Concat(StyleSheets(appUi));
            foreach (string path in sheets)
            {
                foreach (Match m in k_Definition.Matches(File.ReadAllText(path)))
                {
                    defined.Add(m.Groups[1].Value);
                }
            }

            var problems = new List<string>();
            foreach (string path in StyleSheets(StaticAssetScan.Root))
            {
                string[] lines = File.ReadAllLines(path);
                for (int i = 0; i < lines.Length; i++)
                {
                    foreach (Match m in k_Use.Matches(lines[i]))
                    {
                        string name = m.Groups[1].Value;
                        // --unity-* come from the Editor/runtime theme; a var() with a fallback is deliberate
                        if (!defined.Contains(name) && !name.StartsWith("--unity-", StringComparison.Ordinal) && !m.Groups[2].Success)
                        {
                            problems.Add($"{path}:{i + 1} {name}");
                        }
                    }
                }
            }
            StaticAssetScan.AssertNoProblems(problems, "var() reference(s) to undefined variables");
        }

        [Test]
        public void LightAndDarkThemes_DefineTheSameFoodmissionVariables()
        {
            string theme = File.ReadAllText(ThemePath);
            HashSet<string> light = FmVariablesIn(theme, ".appui--light");
            HashSet<string> dark = FmVariablesIn(theme, ".appui--dark");
            Assert.IsNotEmpty(light, ".appui--light defines no --fm-* variables");

            var problems = light.Except(dark).Select(v => $"{v} only in light")
                .Concat(dark.Except(light).Select(v => $"{v} only in dark"))
                .OrderBy(p => p, StringComparer.Ordinal)
                .ToList();
            StaticAssetScan.AssertNoProblems(problems, "--fm-* variable(s) defined for one theme only");
        }

        private static IEnumerable<string> StyleSheets(string root)
        {
            return Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
                .Where(p => p.EndsWith(".uss", StringComparison.OrdinalIgnoreCase) || p.EndsWith(".tss", StringComparison.OrdinalIgnoreCase))
                .Select(p => p.Replace('\\', '/'))
                .OrderBy(p => p, StringComparer.Ordinal);
        }

        private static HashSet<string> FmVariablesIn(string uss, string themeClass)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (Match block in k_Block.Matches(uss))
            {
                if (block.Groups[1].Value.Contains(themeClass))
                {
                    foreach (Match m in k_Definition.Matches(block.Groups[2].Value))
                    {
                        if (m.Groups[1].Value.StartsWith("--fm-", StringComparison.Ordinal))
                        {
                            names.Add(m.Groups[1].Value);
                        }
                    }
                }
            }
            return names;
        }
    }
}
