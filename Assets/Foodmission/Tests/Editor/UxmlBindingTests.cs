using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    /// <summary>
    /// Every binding declared in UXML must point at something the runtime binding system can resolve:
    /// a typo or a renamed property otherwise binds nothing, with no error.
    /// </summary>
    [TestFixture]
    public class UxmlBindingTests
    {
        private const string DataSourceType = "data-source-type";

        [Test]
        public void DataSourcePaths_ExistOnTheirDataSourceType()
        {
            var problems = new List<string>();
            foreach ((string path, XDocument doc) in StaticAssetScan.UxmlDocuments())
            {
                foreach (XElement binding in doc.Descendants().Where(e => e.Name.LocalName == "DataBinding"))
                {
                    string sourcePath = (string)binding.Attribute("data-source-path");
                    if (string.IsNullOrEmpty(sourcePath))
                    {
                        continue;
                    }
                    string where = $"{path}:{StaticAssetScan.Line(binding)}";

                    // The data source type is set on the binding itself or inherited from the nearest ancestor that declares it
                    string typeName = (string)binding.Attribute(DataSourceType)
                        ?? binding.Ancestors().Select(a => (string)a.Attribute(DataSourceType)).FirstOrDefault(t => t != null);
                    if (typeName == null)
                    {
                        // Data source assigned from code: nothing to check statically
                        continue;
                    }
                    Type sourceType = Type.GetType(typeName);
                    if (sourceType == null)
                    {
                        problems.Add($"{where} data-source-type '{typeName}' does not resolve");
                        continue;
                    }
                    string member = sourcePath.Split('.', '[')[0];
                    if (!StaticAssetScan.BindableProperties(sourceType).Contains(member))
                    {
                        problems.Add($"{where} '{member}' is not a bindable property of {sourceType.Name} (missing or no [CreateProperty])");
                    }
                }
            }
            StaticAssetScan.AssertNoProblems(problems, "data-source-path(s) that bind nothing");
        }

        [Test]
        public void BindingTargets_ExistOnTheBoundElement()
        {
            var problems = new List<string>();
            foreach ((string path, XDocument doc) in StaticAssetScan.UxmlDocuments())
            {
                foreach (XElement bindings in doc.Descendants().Where(e => e.Name.LocalName == "Bindings"))
                {
                    XElement element = bindings.Parent;
                    Type elementType = StaticAssetScan.ResolveElementType(element.Name);
                    string where = $"{path}:{StaticAssetScan.Line(element)}";
                    if (elementType == null)
                    {
                        problems.Add($"{where} element type '{element.Name.LocalName}' does not resolve");
                        continue;
                    }
                    HashSet<string> properties = StaticAssetScan.BindableProperties(elementType);
                    foreach (XElement binding in bindings.Elements())
                    {
                        string property = (string)binding.Attribute("property");
                        if (property == null)
                        {
                            continue;
                        }
                        // Nested targets such as "style.display" start at a bindable member of the element
                        string member = property.Split('.', '[')[0];
                        if (!properties.Contains(member))
                        {
                            problems.Add($"{path}:{StaticAssetScan.Line(binding)} {elementType.Name} has no bindable property '{member}'");
                        }
                    }
                }
            }
            StaticAssetScan.AssertNoProblems(problems, "binding target(s) that don't exist");
        }
    }
}
