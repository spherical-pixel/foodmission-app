using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class ExpiredWasteBatcherTests
    {
        private Mock<IPantryService> _mockPantryService;
        private Mock<INotificationService> _mockNotificationService;
        private Mock<IChallengeSessionService> _mockChallengeSession;
        private ExpiredWasteBatcher _batcher;

        [SetUp]
        public void SetUp()
        {
            _mockPantryService = new Mock<IPantryService>();
            _mockNotificationService = new Mock<INotificationService>();
            _mockChallengeSession = new Mock<IChallengeSessionService>();
            _mockChallengeSession
                .Setup(x => x.ReportAsync(It.IsAny<ChallengeCompletionTrigger>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            _batcher = new ExpiredWasteBatcher(_mockPantryService.Object, _mockNotificationService.Object, _mockChallengeSession.Object);
        }

        private void SetupBatch(BatchWasteResult result, ApiErrorResponse error = null)
        {
            _mockPantryService
                .Setup(x => x.BatchWasteAsync(It.IsAny<BatchWasteRequest>()))
                .Returns(Task.FromResult<(BatchWasteResult Result, ApiErrorResponse Error)>((result, error)));
        }

        private static FoodWaste Created(string id, string pantryItemId)
        {
            return new FoodWaste { id = id, pantryItemId = pantryItemId };
        }

        [Test]
        public async Task WasteAsync_NoItems_DoesNotCallApi()
        {
            var (wasted, error) = await _batcher.WasteAsync(new string[0]);

            Assert.AreEqual(0, wasted);
            Assert.IsNull(error);
            _mockPantryService.Verify(x => x.BatchWasteAsync(It.IsAny<BatchWasteRequest>()), Times.Never);
        }

        [Test]
        public async Task WasteAsync_SendsOnlyPantryItemIds()
        {
            BatchWasteRequest sent = null;
            _mockPantryService
                .Setup(x => x.BatchWasteAsync(It.IsAny<BatchWasteRequest>()))
                .Callback<BatchWasteRequest>(r => sent = r)
                .Returns(Task.FromResult<(BatchWasteResult Result, ApiErrorResponse Error)>((new BatchWasteResult(), null)));

            await _batcher.WasteAsync(new[] { "i1", "i2" });

            CollectionAssert.AreEqual(new[] { "i1", "i2" }, sent.items.Select(i => i.pantryItemId).ToArray());
            Assert.IsTrue(sent.items.All(i => i.quantity == null && i.unit == null && i.costEstimate == null && i.notes == null));
        }

        [Test]
        public async Task WasteAsync_Success_CancelsRemindersAndReportsEachSucceededItem()
        {
            SetupBatch(new BatchWasteResult
            {
                successCount = 2,
                successes = new[] { Created("w1", "i1"), Created("w2", "i2") }
            });

            var (wasted, error) = await _batcher.WasteAsync(new[] { "i1", "i2" });

            Assert.AreEqual(2, wasted);
            Assert.IsNull(error);
            _mockNotificationService.Verify(x => x.CancelPantryReminder("i1"), Times.Once);
            _mockNotificationService.Verify(x => x.CancelPantryReminder("i2"), Times.Once);
            _mockChallengeSession.Verify(x => x.ReportAsync(ChallengeCompletionTrigger.FoodWasteLogged, "w1"), Times.Once);
            _mockChallengeSession.Verify(x => x.ReportAsync(ChallengeCompletionTrigger.FoodWasteLogged, "w2"), Times.Once);
        }

        [Test]
        public async Task WasteAsync_PartialFailure_ReturnsErrorWithBackendMessages()
        {
            SetupBatch(new BatchWasteResult
            {
                successCount = 1,
                errorCount = 1,
                successes = new[] { Created("w1", "i1") },
                errors = new[] { new BatchWasteErrorItem { pantryItemId = "i2", error = "Pantry item not found" } }
            });

            var (wasted, error) = await _batcher.WasteAsync(new[] { "i1", "i2" });

            Assert.AreEqual(1, wasted);
            Assert.IsNotNull(error);
            StringAssert.Contains("Pantry item not found", error.message);
            _mockNotificationService.Verify(x => x.CancelPantryReminder("i2"), Times.Never);
            _mockChallengeSession.Verify(x => x.ReportAsync(It.IsAny<ChallengeCompletionTrigger>(), It.IsAny<string>()), Times.Once);
        }

        [Test]
        public async Task WasteAsync_RequestError_ReturnsItAndTouchesNothing()
        {
            var apiError = new ApiErrorResponse { message = "down" };
            SetupBatch(null, apiError);

            var (wasted, error) = await _batcher.WasteAsync(new[] { "i1" });

            Assert.AreEqual(0, wasted);
            Assert.AreSame(apiError, error);
            _mockNotificationService.Verify(x => x.CancelPantryReminder(It.IsAny<string>()), Times.Never);
            _mockChallengeSession.Verify(x => x.ReportAsync(It.IsAny<ChallengeCompletionTrigger>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task WasteAsync_Exception_LogsAndReturnsZero()
        {
            _mockPantryService
                .Setup(x => x.BatchWasteAsync(It.IsAny<BatchWasteRequest>()))
                .Returns(Task.FromException<(BatchWasteResult Result, ApiErrorResponse Error)>(new Exception("boom")));
            LogAssert.Expect(LogType.Error, new Regex(@"\[ExpiredWasteBatcher\]"));

            var (wasted, error) = await _batcher.WasteAsync(new[] { "i1" });

            Assert.AreEqual(0, wasted);
            Assert.IsNull(error);
            _mockNotificationService.Verify(x => x.CancelPantryReminder(It.IsAny<string>()), Times.Never);
        }
    }
}
