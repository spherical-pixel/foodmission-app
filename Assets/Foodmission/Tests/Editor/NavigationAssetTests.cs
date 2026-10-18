using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using NUnit.Framework;

using Unity.AppUI.Navigation;

using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.UIElements;

namespace eu.foodmission.platform.Tests
{
    /// <summary>
    /// The navigation graph and the Addressables templates are wired by strings:
    /// a renamed screen class or a moved UXML only fails when the user reaches that screen.
    /// </summary>
    [TestFixture]
    public class NavigationAssetTests
    {
        private const string NavGraphPath = "Assets/Foodmission/AppUI/Foodmission_NavigationGraph.asset";

        private static List<NavDestination> Destinations()
        {
            var graph = AssetDatabase.LoadAssetAtPath<NavGraphViewAsset>(NavGraphPath);
            Assert.IsNotNull(graph, NavGraphPath);
            return graph.nodes.OfType<NavDestination>().ToList();
        }

        [Test]
        public void EveryDestination_OpensAScreenClassThatExists()
        {
            var problems = new List<string>();
            foreach (NavDestination destination in Destinations())
            {
                if (!(destination.destinationTemplate is DefaultDestinationTemplate template))
                {
                    continue;
                }
                Type screen = Type.GetType(template.template);
                if (screen == null)
                {
                    problems.Add($"{destination.name}: screen type '{template.template}' does not resolve");
                }
                else if (!typeof(NavigationScreen).IsAssignableFrom(screen) || screen.GetConstructor(Type.EmptyTypes) == null)
                {
                    // DefaultDestinationTemplate does Activator.CreateInstance(type) as NavigationScreen
                    problems.Add($"{destination.name}: {screen.Name} is not a NavigationScreen with a public parameterless constructor");
                }
            }
            StaticAssetScan.AssertNoProblems(problems, "destination(s) without a usable screen");
        }

        [Test]
        public void DestinationsWithAppBar_HaveALocalizedTitle()
        {
            // SetupAppBar shows destination.label as is: plain text would appear untranslated in every language
            SharedTableData ui = StaticAssetScan.SharedTable(StaticAssetScan.UiTable);
            var problems = new List<string>();
            foreach (NavDestination destination in Destinations())
            {
                if (!(destination.destinationTemplate is DefaultDestinationTemplate template) || !template.showAppBar)
                {
                    continue;
                }
                string label = destination.label ?? "";
                if (!label.StartsWith("@UI:", StringComparison.Ordinal))
                {
                    problems.Add($"{destination.name}: AppBar title \"{label}\" is not a @UI: key");
                }
                else if (ui.GetEntry(label.Substring("@UI:".Length)) == null)
                {
                    problems.Add($"{destination.name}: {label} is not in the UI table");
                }
            }
            StaticAssetScan.AssertNoProblems(problems, "AppBar title(s) that won't be translated");
        }

        [Test]
        public void EveryTemplateAddress_IsAnAddressableUxml()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            Assert.IsNotNull(settings, "Addressables settings not found");
            Dictionary<string, AddressableAssetEntry> byAddress = settings.groups
                .Where(g => g != null)
                .SelectMany(g => g.entries)
                .GroupBy(e => e.address)
                .ToDictionary(g => g.Key, g => g.First());

            var problems = new List<string>();
            foreach (FieldInfo field in typeof(TemplateAddresses).GetFields(BindingFlags.Public | BindingFlags.Static).Where(f => f.IsLiteral))
            {
                string address = (string)field.GetRawConstantValue();
                if (!byAddress.TryGetValue(address, out AddressableAssetEntry entry))
                {
                    problems.Add($"TemplateAddresses.{field.Name}: no Addressables entry '{address}'");
                }
                else if (AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(entry.AssetPath) == null)
                {
                    problems.Add($"TemplateAddresses.{field.Name}: '{address}' points at {entry.AssetPath}, not a UXML");
                }
            }
            StaticAssetScan.AssertNoProblems(problems, "template address(es) that won't load");
        }
    }
}
