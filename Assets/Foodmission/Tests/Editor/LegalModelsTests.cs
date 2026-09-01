using System.Text;
using Newtonsoft.Json;
using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class LegalModelsTests
    {
        [Test]
        public void LegalDocType_Constants_ShouldMatchExpected()
        {
            Assert.AreEqual("TERMS_OF_SERVICE", LegalDocType.TermsOfService);
            Assert.AreEqual("PRIVACY_POLICY", LegalDocType.PrivacyPolicy);
        }

        [Test]
        public void LegalDocument_Deserialization_PopulatesAllFields()
        {
            string json = @"{
                ""key"": ""TERMS_OF_SERVICE:1.0:es"",
                ""docType"": ""TERMS_OF_SERVICE"",
                ""version"": ""1.0"",
                ""title"": ""Términos de Servicio"",
                ""content"": ""# Términos de Servicio\n\nBienvenido a Foodmission..."",
                ""locale"": ""es"",
                ""updatedAt"": ""2026-08-26T10:00:00.000Z""
            }";

            var doc = JsonConvert.DeserializeObject<LegalDocument>(json);

            Assert.IsNotNull(doc);
            Assert.AreEqual("TERMS_OF_SERVICE:1.0:es", doc.key);
            Assert.AreEqual("TERMS_OF_SERVICE", doc.docType);
            Assert.AreEqual("1.0", doc.version);
            Assert.AreEqual("Términos de Servicio", doc.title);
            Assert.IsTrue(doc.content.Contains("Bienvenido a Foodmission"));
            Assert.AreEqual("es", doc.locale);
            Assert.AreEqual("2026-08-26T10:00:00.000Z", doc.updatedAt);
        }

        [Test]
        public void LegalConsentStatus_Deserialization_PopulatesMustAcceptAndDocuments()
        {
            string json = @"{
                ""mustAccept"": true,
                ""documents"": [
                    {
                        ""docType"": ""TERMS_OF_SERVICE"",
                        ""documentKey"": ""TERMS_OF_SERVICE:1.0:de"",
                        ""requiredVersion"": ""1.0"",
                        ""locale"": ""de"",
                        ""accepted"": false,
                        ""acceptedVersion"": null,
                        ""acceptedAt"": null
                    },
                    {
                        ""docType"": ""PRIVACY_POLICY"",
                        ""documentKey"": ""PRIVACY_POLICY:1.0:de"",
                        ""requiredVersion"": ""1.0"",
                        ""locale"": ""de"",
                        ""accepted"": true,
                        ""acceptedVersion"": ""1.0"",
                        ""acceptedAt"": ""2026-08-26T12:00:00.000Z""
                    }
                ]
            }";

            var status = JsonConvert.DeserializeObject<LegalConsentStatus>(json);

            Assert.IsNotNull(status);
            Assert.IsTrue(status.mustAccept);
            Assert.IsNotNull(status.documents);
            Assert.AreEqual(2, status.documents.Length);
            Assert.IsFalse(status.documents[0].accepted);
            Assert.IsTrue(status.documents[1].accepted);
            Assert.AreEqual("TERMS_OF_SERVICE", status.documents[0].docType);
            Assert.AreEqual("PRIVACY_POLICY", status.documents[1].docType);
        }

        [Test]
        public void AcceptLegalConsentRequest_ToJsonBody_ProducesValidJson()
        {
            var req = new AcceptLegalConsentRequest
            {
                documentKey = "TERMS_OF_SERVICE:1.0:es"
            };

            byte[] bytes = req.ToJsonBody();
            string json = Encoding.UTF8.GetString(bytes);

            Assert.IsTrue(json.Contains("\"documentKey\":\"TERMS_OF_SERVICE:1.0:es\""));
        }

        [Test]
        public void AcceptLegalConsentResponse_Deserialization_PopulatesFields()
        {
            string json = @"{
                ""accepted"": true,
                ""userId"": ""user-uuid-123"",
                ""documentKey"": ""TERMS_OF_SERVICE:1.0:es"",
                ""docType"": ""TERMS_OF_SERVICE"",
                ""version"": ""1.0"",
                ""locale"": ""es"",
                ""acceptedAt"": ""2026-08-26T14:30:00.000Z""
            }";

            var res = JsonConvert.DeserializeObject<AcceptLegalConsentResponse>(json);

            Assert.IsNotNull(res);
            Assert.IsTrue(res.accepted);
            Assert.AreEqual("user-uuid-123", res.userId);
            Assert.AreEqual("TERMS_OF_SERVICE:1.0:es", res.documentKey);
            Assert.AreEqual("1.0", res.version);
            Assert.AreEqual("es", res.locale);
        }
    }
}
