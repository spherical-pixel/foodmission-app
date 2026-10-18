using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

using NUnit.Framework;

using Unity.Properties;

using UnityEditor;
using UnityEngine.Localization.Tables;

namespace eu.foodmission.platform.Tests
{
    /// <summary>
    /// Shared helpers for the static asset checks: they read UXML, USS, C# and table assets from disk
    /// and never instantiate screens, so a broken reference fails here instead of silently at runtime.
    /// </summary>
    internal static class StaticAssetScan
    {
        public const string Root = "Assets/Foodmission";
        public const string UiTable = "UI";

        private static Dictionary<string, Type> s_TypesByFullName;

        public static IEnumerable<string> FilesUnderRoot(params string[] extensions)
        {
            return Directory.EnumerateFiles(Root, "*", SearchOption.AllDirectories)
                .Where(p => extensions.Any(e => p.EndsWith(e, StringComparison.OrdinalIgnoreCase)))
                .Select(p => p.Replace('\\', '/'))
                .OrderBy(p => p, StringComparer.Ordinal);
        }

        public static IEnumerable<(string Path, XDocument Doc)> UxmlDocuments()
        {
            foreach (string path in FilesUnderRoot(".uxml"))
            {
                yield return (path, XDocument.Load(path, LoadOptions.SetLineInfo));
            }
        }

        public static int Line(XObject node)
        {
            return ((System.Xml.IXmlLineInfo)node).LineNumber;
        }

        /// <summary>Resolves a UXML element name ("ui:Label", "eu.foodmission.platform.Components.X") to its C# type.</summary>
        public static Type ResolveElementType(XName name)
        {
            string ns = name.NamespaceName;
            string local = name.LocalName;
            if (!string.IsNullOrEmpty(ns) && TryGetType(ns + "." + local, out Type type))
            {
                return type;
            }
            return TryGetType(local, out type) ? type : null;
        }

        public static bool TryGetType(string fullName, out Type type)
        {
            if (s_TypesByFullName == null)
            {
                s_TypesByFullName = new Dictionary<string, Type>(StringComparer.Ordinal);
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    Type[] types;
                    try
                    {
                        types = assembly.GetTypes();
                    }
                    catch (ReflectionTypeLoadException e)
                    {
                        types = e.Types.Where(t => t != null).ToArray();
                    }
                    foreach (Type t in types)
                    {
                        if (t.FullName != null && !s_TypesByFullName.ContainsKey(t.FullName))
                        {
                            s_TypesByFullName.Add(t.FullName, t);
                        }
                    }
                }
            }
            return s_TypesByFullName.TryGetValue(fullName, out type);
        }

        /// <summary>
        /// Names the runtime binding system can see on a type: its Unity.Properties property bag
        /// (public fields and [CreateProperty] members), not every public C# property.
        /// </summary>
        public static HashSet<string> BindableProperties(Type type)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            IPropertyBag bag = PropertyBag.GetPropertyBag(type);
            if (bag == null)
            {
                return names;
            }
            MethodInfo getProperties = typeof(IPropertyBag<>).MakeGenericType(type).GetMethod("GetProperties", Type.EmptyTypes);
            foreach (object property in (IEnumerable)getProperties.Invoke(bag, null))
            {
                names.Add(((IProperty)property).Name);
            }
            return names;
        }

        public static IEnumerable<SharedTableData> SharedTables()
        {
            return AssetDatabase.FindAssets("t:SharedTableData", new[] { Root })
                .Select(guid => AssetDatabase.LoadAssetAtPath<SharedTableData>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(t => t != null);
        }

        public static SharedTableData SharedTable(string collectionName)
        {
            SharedTableData table = SharedTables().SingleOrDefault(t => t.TableCollectionName == collectionName);
            Assert.IsNotNull(table, $"Shared table data for '{collectionName}' not found under {Root}");
            return table;
        }

        public static List<StringTable> StringTables(SharedTableData shared)
        {
            List<StringTable> tables = AssetDatabase.FindAssets("t:StringTable", new[] { Root })
                .Select(guid => AssetDatabase.LoadAssetAtPath<StringTable>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(t => t != null && t.SharedData == shared)
                .ToList();
            Assert.IsNotEmpty(tables, $"No locale tables found for '{shared.TableCollectionName}'");
            return tables;
        }

        public static void AssertNoProblems(List<string> problems, string what)
        {
            Assert.IsEmpty(problems, $"{problems.Count} {what}:\n" + string.Join("\n", problems));
        }
    }
}
