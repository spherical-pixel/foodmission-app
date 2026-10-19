using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using NUnit.Framework;

using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation;

using UnityEditor;
using UnityEngine;

namespace eu.foodmission.platform.Tests
{
    /// <summary>
    /// Checks the DI registrations of FoodmissionAppBuilder without building any service (their constructors do I/O):
    /// a missing registration or a cycle only throws when the screen that needs it is opened on a device.
    /// </summary>
    [TestFixture]
    public class DependencyGraphTests
    {
        private const string NavGraphPath = "Assets/Foodmission/AppUI/Foodmission_NavigationGraph.asset";

        private Dictionary<Type, ServiceDescriptor> _services;

        [OneTimeSetUp]
        public void ReadRegistrations()
        {
            AppBuilder builder = AppBuilder.InstantiateWith<FoodmissionApp, UIToolkitHost>();
            var host = new GameObject("DependencyGraphTests");
            try
            {
                var appBuilder = host.AddComponent<FoodmissionAppBuilder>();
                typeof(FoodmissionAppBuilder)
                    .GetMethod("OnConfiguringApp", BindingFlags.NonPublic | BindingFlags.Instance)
                    .Invoke(appBuilder, new object[] { builder });
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
            _services = builder.services.GroupBy(d => d.serviceType).ToDictionary(g => g.Key, g => g.Last());
            Assert.IsTrue(_services.ContainsKey(typeof(IStoreService)) && _services.ContainsKey(typeof(HomeScreenViewModel)),
                "FoodmissionAppBuilder registrations were not read");
        }

        [Test]
        public void EveryService_HasAConstructorTheContainerCanFill()
        {
            var problems = new List<string>();
            foreach (ServiceDescriptor descriptor in _services.Values)
            {
                foreach (Type dependency in Dependencies(descriptor, problems))
                {
                    if (!_services.ContainsKey(dependency))
                    {
                        problems.Add($"{descriptor.implementationType.Name} needs {dependency.Name}, which is not registered");
                    }
                }
            }
            StaticAssetScan.AssertNoProblems(problems, "service(s) the container can't build");
        }

        [Test]
        public void Services_HaveNoDependencyCycles()
        {
            var problems = new List<string>();
            var done = new HashSet<Type>();
            foreach (Type service in _services.Keys)
            {
                FindCycle(service, new List<Type>(), done, problems);
            }
            StaticAssetScan.AssertNoProblems(problems.Distinct().ToList(), "dependency cycle(s) (stack overflow when resolved)");
        }

        [Test]
        public void Singletons_DontKeepATransientViewModel()
        {
            var problems = new List<string>();
            foreach (ServiceDescriptor descriptor in _services.Values.Where(d => d.lifetime == ServiceLifetime.Singleton))
            {
                foreach (Type dependency in Dependencies(descriptor, new List<string>()))
                {
                    if (_services.TryGetValue(dependency, out ServiceDescriptor used) && used.lifetime == ServiceLifetime.Transient)
                    {
                        problems.Add($"{descriptor.implementationType.Name} (singleton) captures {dependency.Name} (transient) for the whole app lifetime");
                    }
                }
            }
            StaticAssetScan.AssertNoProblems(problems, "singleton(s) holding a transient");
        }

        [Test]
        public void EveryNavigableScreen_HasItsViewModelRegistered()
        {
            var graph = AssetDatabase.LoadAssetAtPath<NavGraphViewAsset>(NavGraphPath);
            Assert.IsNotNull(graph, NavGraphPath);
            var problems = new List<string>();
            foreach (NavDestination destination in graph.nodes.OfType<NavDestination>())
            {
                Type screen = destination.destinationTemplate is DefaultDestinationTemplate template ? Type.GetType(template.template) : null;
                Type viewModel = screen == null ? null : ViewModelOf(screen);
                if (viewModel != null && !_services.ContainsKey(viewModel))
                {
                    // NavigationScreenBase resolves it with GetRequiredService when the screen opens
                    problems.Add($"{destination.name}: {screen.Name} needs {viewModel.Name}, which is not registered");
                }
            }
            StaticAssetScan.AssertNoProblems(problems, "screen(s) that would throw when opened");
        }

        /// <summary>Constructor parameters and [Service] members, using the constructor App UI's ServiceProvider picks.</summary>
        private IEnumerable<Type> Dependencies(ServiceDescriptor descriptor, List<string> problems)
        {
            Type type = descriptor.implementationType;
            ConstructorInfo[] constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            // ServiceProvider takes the first constructor whose parameters are all assignable from a registered service type
            ConstructorInfo chosen = constructors.FirstOrDefault(c => c.GetParameters().All(p => _services.Keys.Any(s => s.IsAssignableFrom(p.ParameterType))))
                ?? constructors.OrderBy(c => c.GetParameters().Length).FirstOrDefault();
            if (chosen == null)
            {
                problems.Add($"{type.Name} has no public constructor");
                return Enumerable.Empty<Type>();
            }
            const BindingFlags members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            IEnumerable<Type> injected = type.GetFields(members).Where(f => f.GetCustomAttribute<ServiceAttribute>() != null).Select(f => f.FieldType)
                .Concat(type.GetProperties(members).Where(p => p.GetCustomAttribute<ServiceAttribute>() != null).Select(p => p.PropertyType));
            // Then GetService looks each one up by its exact type
            return chosen.GetParameters().Select(p => p.ParameterType).Concat(injected);
        }

        private void FindCycle(Type service, List<Type> path, HashSet<Type> done, List<string> problems)
        {
            int index = path.IndexOf(service);
            if (index >= 0)
            {
                problems.Add(string.Join(" -> ", path.Skip(index).Append(service).Select(t => t.Name)));
                return;
            }
            if (done.Contains(service) || !_services.TryGetValue(service, out ServiceDescriptor descriptor))
            {
                return;
            }
            path.Add(service);
            foreach (Type dependency in Dependencies(descriptor, new List<string>()))
            {
                FindCycle(dependency, path, done, problems);
            }
            path.RemoveAt(path.Count - 1);
            done.Add(service);
        }

        private static Type ViewModelOf(Type screen)
        {
            for (Type t = screen; t != null; t = t.BaseType)
            {
                if (t.IsGenericType && (t.GetGenericTypeDefinition() == typeof(NavigationScreenBase<>) || t.GetGenericTypeDefinition() == typeof(StepFlowScreenBase<>)))
                {
                    return t.GetGenericArguments()[0];
                }
            }
            return null;
        }
    }
}
