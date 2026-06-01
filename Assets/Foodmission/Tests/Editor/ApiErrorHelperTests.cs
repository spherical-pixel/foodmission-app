using System.Text.RegularExpressions;

using NUnit.Framework;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.TestTools;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class ApiErrorHelperTests
    {
        [Test]
        public void Parse_WithNullDownloadHandler_DoesNotThrow()
        {
            using var request = new UnityWebRequest();

            LogAssert.Expect(LogType.Error, new Regex("TestContext: 0 "));
            ApiErrorResponse result = null;
            Assert.DoesNotThrow(() => result = ApiErrorHelper.Parse(request, "TestContext"));

            Assert.IsNotNull(result);
        }

        [Test]
        public void Parse_WithNullDownloadHandler_ReturnsSyntheticError()
        {
            using var request = new UnityWebRequest();

            LogAssert.Expect(LogType.Error, new Regex("TestContext: 0 "));
            var result = ApiErrorHelper.Parse(request, "TestContext");

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.statusCode);
            Assert.AreEqual("HTTP 0", result.message);
            Assert.AreEqual("", result.error);
        }
    }
}
