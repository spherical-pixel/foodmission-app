using System.Collections.Generic;
using System.IO;
using System.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    /// <summary>
    /// AppState rules that break silently: a field missing from Copy() is wiped by the next dispatch,
    /// a field the serializer can't write is lost on restart, and a renamed field loses the value users already have saved.
    /// </summary>
    [TestFixture]
    public class AppStateContractTests
    {
        // Written with the 0.0.8 release (first build in the pilot): what users have saved on their devices
        private const string SavedStateFixture = "Assets/Foodmission/Tests/Editor/Data/app_state_0.0.8.json";

        [Test]
        public void Copy_KeepsEveryField()
        {
            AppState state = StateSamples.Populated<AppState>();

            Assert.AreEqual(StateSamples.Json(state), StateSamples.Json(state.Copy()),
                "A field is missing from AppState.Copy(): every reducer would reset it");
        }

        [Test]
        public void Copy_SharesNoMutableObjectWithTheOriginal()
        {
            AppState state = StateSamples.Populated<AppState>();

            List<string> shared = StateSamples.SharedMutableObjects(state, state.Copy());

            // Reducers edit the copy: anything shared would also change the previous state
            StaticAssetScan.AssertNoProblems(shared, "object(s) shared between a state and its Copy()");
        }

        [Test]
        public void Persistence_KeepsEveryField()
        {
            AppState state = StateSamples.Populated<AppState>();
            string saved = JsonConvert.SerializeObject(state);

            AppState restored = JsonConvert.DeserializeObject<AppState>(saved);

            Assert.AreEqual(StateSamples.Json(state), StateSamples.Json(restored),
                "A field doesn't survive the save/restore done by LocalStorageService");
        }

        [Test]
        public void StateSavedByPreviousRelease_StillLoadsEveryValue()
        {
            JObject saved = JObject.Parse(File.ReadAllText(SavedStateFixture));

            AppState restored = JsonConvert.DeserializeObject<AppState>(saved.ToString());
            JObject reloaded = JObject.FromObject(restored);

            // New fields are fine; a removed, renamed or retyped one loses what users already have saved
            var problems = new List<string>();
            foreach (JProperty property in saved.Properties())
            {
                JToken now = reloaded[property.Name];
                if (now == null)
                {
                    problems.Add($"{property.Name}: no longer in AppState (needs a migration)");
                }
                else if (!JToken.DeepEquals(property.Value, now))
                {
                    problems.Add($"{property.Name}: saved {property.Value.ToString(Formatting.None)} loads as {now.ToString(Formatting.None)}");
                }
            }
            Assert.Greater(saved.Properties().Count(), 0, SavedStateFixture);
            StaticAssetScan.AssertNoProblems(problems, "saved value(s) that a user updating the app would lose");
        }
    }
}
