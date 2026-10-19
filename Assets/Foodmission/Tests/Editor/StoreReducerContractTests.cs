using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using NUnit.Framework;

using Unity.AppUI.Redux;

namespace eu.foodmission.platform.Tests
{
    /// <summary>
    /// Every AppActions creator must reach a reducer (an unhandled action is silently ignored) and reducers must
    /// return a new state without touching the previous one (subscribers compare old and new).
    /// </summary>
    [TestFixture]
    public class StoreReducerContractTests
    {
        private static IEnumerable<FieldInfo> ActionCreators()
        {
            return typeof(AppActions).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => typeof(IActionCreator).IsAssignableFrom(f.FieldType));
        }

        private static IAction CreateAction(FieldInfo field)
        {
            object creator = field.GetValue(null);
            MethodInfo invoke = creator.GetType().GetMethods().First(m => m.Name == "Invoke" && m.GetParameters().Length <= 1);
            ParameterInfo[] parameters = invoke.GetParameters();
            object[] args = parameters.Length == 0 ? new object[0] : new[] { StateSamples.Sample(parameters[0].ParameterType, field.Name + "-payload") };
            return (IAction)invoke.Invoke(creator, args);
        }

        private static List<string> CheckEveryAction(IStore<AppState> store)
        {
            var problems = new List<string>();
            foreach (FieldInfo field in ActionCreators())
            {
                AppState before = store.GetState();
                string beforeJson = StateSamples.Json(before);

                store.Dispatch(CreateAction(field));

                if (ReferenceEquals(before, store.GetState()))
                {
                    problems.Add($"{field.Name}: no reducer handled it (or it returned the same state object)");
                }
                if (StateSamples.Json(before) != beforeJson)
                {
                    problems.Add($"{field.Name}: the reducer modified the previous state instead of a copy");
                }
            }
            return problems;
        }

        [Test]
        public void AppStore_HandlesEveryActionWithoutMutatingThePreviousState()
        {
            var storage = new TestLocalStorageService();
            storage.SetValue(StoreService.APP_STATE_KEY, StateSamples.Populated<AppState>());
            var service = new StoreService(storage);
            try
            {
                Assert.Greater(ActionCreators().Count(), 0);
                StaticAssetScan.AssertNoProblems(CheckEveryAction(service.store), "action(s) breaking the store contract");
            }
            finally
            {
                service.Dispose();
            }
        }

        [Test]
        public void TestStoreService_HandlesTheSameActionsAsTheAppStore()
        {
            // TestStoreService keeps its own copy of the reducer list: a new action missing there makes VM tests lie
            var testStore = new TestStoreService();
            try
            {
                testStore.SetAppState(StateSamples.Populated<AppState>());
                StaticAssetScan.AssertNoProblems(CheckEveryAction(testStore.store), "action(s) TestStoreService doesn't handle like the app");
            }
            finally
            {
                testStore.Dispose();
            }
        }
    }
}
