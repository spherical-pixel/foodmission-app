using System.Linq;

using NUnit.Framework;

using Unity.AppUI.Navigation;

using UnityEditor;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class KnowledgeBarNavigationTests
    {
        private const string GraphPath = "Assets/Foodmission/AppUI/Foodmission_NavigationGraph.asset";

        [Test]
        public void Destination_HasLocalizedAppBarTitle()
        {
            var asset = AssetDatabase.LoadAssetAtPath<NavGraphViewAsset>(GraphPath);
            NavDestination destination = asset.nodes.OfType<NavDestination>().Single(n => n.name == "KnowledgeBarDetail");

            // The AppBar shows destination.label as is: it must be a UI table key, not the node name
            Assert.AreEqual("@UI:KNOWLEDGE", destination.label);
        }
    }
}
