using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class QuestExtensionsTests
    {
        [Test]
        public void GetDisplayName_PrefersNameOverGenericTitle()
        {
            var quest = new Quest
            {
                code = "QUEST.DIET_CHANGES.BEGINNER.1",
                title = "Diet changes - Beginner",
                name = "Learn to Log Your Food"
            };

            Assert.AreEqual("Learn to Log Your Food", quest.GetDisplayName());
        }

        [Test]
        public void GetDisplayName_FallsBackToTitleWhenNameIsEmpty()
        {
            var quest = new Quest { code = "QUEST.DIET_CHANGES.BEGINNER.1", title = "Diet changes - Beginner", name = "" };

            Assert.AreEqual("Diet changes - Beginner", quest.GetDisplayName());
        }

        [Test]
        public void GetDisplayName_FallsBackToCodeWhenNameAndTitleAreEmpty()
        {
            var quest = new Quest { code = "QUEST.DIET_CHANGES.BEGINNER.1" };

            Assert.AreEqual("QUEST.DIET_CHANGES.BEGINNER.1", quest.GetDisplayName());
        }

        [Test]
        public void GetDisplayName_WhenQuestIsNull_ReturnsEmpty()
        {
            Quest quest = null;

            Assert.AreEqual(string.Empty, quest.GetDisplayName());
        }
    }
}
