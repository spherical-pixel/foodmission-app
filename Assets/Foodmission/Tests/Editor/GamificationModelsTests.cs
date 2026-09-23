using System;
using Newtonsoft.Json;
using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class GamificationModelsTests
    {
        [Test]
        public void WalletBalance_Deserialization_ShouldPopulateAllFields()
        {
            string json = @"{
                ""xp"": 150,
                ""points"": 75,
                ""updatedAt"": ""2026-09-14T12:00:00.000Z""
            }";

            var wallet = JsonConvert.DeserializeObject<WalletBalance>(json);

            Assert.IsNotNull(wallet);
            Assert.AreEqual(150, wallet.xp);
            Assert.AreEqual(75, wallet.points);
            Assert.AreEqual("2026-09-14T12:00:00.000Z", wallet.updatedAt);
        }

        [Test]
        public void ContentReward_Deserialization_ShouldPopulateFields()
        {
            string json = @"{
                ""xp"": 20,
                ""points"": 15,
                ""badgeId"": ""badge-uuid-123"",
                ""avatarItem"": ""hat_chef"",
                ""petItem"": null,
                ""collectible"": ""badge_plant_master""
            }";

            var reward = JsonConvert.DeserializeObject<ContentReward>(json);

            Assert.IsNotNull(reward);
            Assert.AreEqual(20, reward.xp);
            Assert.AreEqual(15, reward.points);
            Assert.AreEqual("badge-uuid-123", reward.badgeId);
            Assert.AreEqual("hat_chef", reward.avatarItem);
            Assert.IsNull(reward.petItem);
            Assert.AreEqual("badge_plant_master", reward.collectible);
        }

        [Test]
        public void WalletEntry_Deserialization_ShouldPopulateAllFields()
        {
            string json = @"{
                ""id"": ""entry-uuid-1"",
                ""currency"": ""XP"",
                ""amount"": 20,
                ""balanceAfter"": 120,
                ""reason"": ""Quiz Q1.1 completed"",
                ""eventId"": ""event-uuid-9"",
                ""createdAt"": ""2026-09-14T12:00:00.000Z""
            }";

            var entry = JsonConvert.DeserializeObject<WalletEntry>(json);

            Assert.IsNotNull(entry);
            Assert.AreEqual("entry-uuid-1", entry.id);
            Assert.AreEqual("XP", entry.currency);
            Assert.AreEqual(20, entry.amount);
            Assert.AreEqual(120, entry.balanceAfter);
            Assert.AreEqual("Quiz Q1.1 completed", entry.reason);
            Assert.AreEqual("event-uuid-9", entry.eventId);
        }

        [Test]
        public void ProgressIndicator_Deserialization_ShouldPopulateAllFields()
        {
            string json = @"{
                ""id"": ""pi-uuid-1"",
                ""kind"": ""DIET_CHANGES"",
                ""precision"": ""EXACT"",
                ""level"": 2,
                ""accumulatedValue"": 14.5,
                ""targetValue"": 20.0,
                ""allTimeTotal"": 85.0,
                ""cycleStartedAt"": ""2026-09-01T00:00:00.000Z"",
                ""lastUpdatedAt"": ""2026-09-14T10:00:00.000Z""
            }";

            var indicator = JsonConvert.DeserializeObject<ProgressIndicator>(json);

            Assert.IsNotNull(indicator);
            Assert.AreEqual("pi-uuid-1", indicator.id);
            Assert.AreEqual("DIET_CHANGES", indicator.kind);
            Assert.AreEqual("EXACT", indicator.precision);
            Assert.AreEqual(2, indicator.level);
            Assert.AreEqual(14.5f, indicator.accumulatedValue);
            Assert.AreEqual(20.0f, indicator.targetValue);
            Assert.AreEqual(85.0f, indicator.allTimeTotal);
        }

        [Test]
        public void UserEarnedRewardsResponse_Deserialization_ShouldPopulateEarnedRewardsAndWallet()
        {
            string json = @"{
                ""earnedRewards"": [
                    {
                        ""id"": ""earned-1"",
                        ""sourceType"": ""QUIZ"",
                        ""sourceId"": ""quiz-uuid-1"",
                        ""earnedAt"": ""2026-09-14T12:00:00.000Z"",
                        ""reward"": {
                            ""id"": ""rew-1"",
                            ""name"": ""Quiz Reward"",
                            ""points"": 10,
                            ""xp"": 15,
                            ""badgeId"": null
                        }
                    }
                ],
                ""wallet"": {
                    ""xp"": 15,
                    ""points"": 10,
                    ""updatedAt"": ""2026-09-14T12:00:00.000Z""
                }
            }";

            var response = JsonConvert.DeserializeObject<UserEarnedRewardsResponse>(json);

            Assert.IsNotNull(response);
            Assert.IsNotNull(response.wallet);
            Assert.AreEqual(15, response.wallet.xp);
            Assert.AreEqual(1, response.earnedRewards.Length);
            Assert.AreEqual("earned-1", response.earnedRewards[0].id);
            Assert.AreEqual("QUIZ", response.earnedRewards[0].sourceType);
            Assert.AreEqual("Quiz Reward", response.earnedRewards[0].reward.name);
        }

        [Test]
        public void FoodFactProgressResponse_Deserialization_ShouldPopulateFieldsAndReward()
        {
            string json = @"{
                ""id"": ""ff-prog-1"",
                ""userId"": ""user-uuid-1"",
                ""foodFactId"": ""ff-uuid-123"",
                ""foodFactCode"": ""FF1.1.1"",
                ""readAt"": ""2026-09-14T12:00:00.000Z"",
                ""reward"": {
                    ""xp"": 10,
                    ""points"": 15
                }
            }";

            var response = JsonConvert.DeserializeObject<FoodFactProgressResponse>(json);

            Assert.IsNotNull(response);
            Assert.AreEqual("ff-prog-1", response.id);
            Assert.AreEqual("user-uuid-1", response.userId);
            Assert.AreEqual("FF1.1.1", response.foodFactCode);
            Assert.IsNotNull(response.reward);
            Assert.AreEqual(10, response.reward.xp);
            Assert.AreEqual(15, response.reward.points);
        }

        [Test]
        public void ContentProgress_Models_ShouldIncludeRewardProperty()
        {
            string quizJson = @"{
                ""id"": ""prog-q"",
                ""userId"": ""u-1"",
                ""quizId"": ""q-1"",
                ""quizCode"": ""Q1.1"",
                ""completed"": true,
                ""reward"": { ""xp"": 20, ""points"": 10 }
            }";
            var quizProg = JsonConvert.DeserializeObject<QuizProgress>(quizJson);
            Assert.IsNotNull(quizProg);
            Assert.IsNotNull(quizProg.reward);
            Assert.AreEqual(20, quizProg.reward.xp);

            string questJson = @"{
                ""id"": ""prog-qst"",
                ""userId"": ""u-1"",
                ""questId"": ""qst-1"",
                ""questCode"": ""QUEST.1"",
                ""completed"": true,
                ""progress"": 100,
                ""reward"": { ""xp"": 50, ""points"": 30 }
            }";
            var questProg = JsonConvert.DeserializeObject<QuestProgress>(questJson);
            Assert.IsNotNull(questProg);
            Assert.IsNotNull(questProg.reward);
            Assert.AreEqual(50, questProg.reward.xp);

            string missionJson = @"{
                ""missionId"": ""m-1"",
                ""userId"": ""u-1"",
                ""progress"": 100,
                ""completed"": true,
                ""missionTitle"": ""Zero Waste"",
                ""reward"": { ""xp"": 30, ""points"": 25 }
            }";
            var missionProg = JsonConvert.DeserializeObject<MissionProgress>(missionJson);
            Assert.IsNotNull(missionProg);
            Assert.IsNotNull(missionProg.reward);
            Assert.AreEqual(30, missionProg.reward.xp);

            string challengeJson = @"{
                ""challengeId"": ""c-1"",
                ""userId"": ""u-1"",
                ""progress"": 100,
                ""completed"": true,
                ""challengeTitle"": ""Daily Veggie"",
                ""reward"": { ""xp"": 15, ""points"": 10 }
            }";
            var challengeProg = JsonConvert.DeserializeObject<ChallengeProgress>(challengeJson);
            Assert.IsNotNull(challengeProg);
            Assert.IsNotNull(challengeProg.reward);
            Assert.AreEqual(15, challengeProg.reward.xp);
        }
    }
}
