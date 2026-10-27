using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class RegisterErrorMapperTests
    {
        [Test]
        public void Map_KeycloakInvalidUsername_ReturnsUsernameInvalid()
        {
            string body = "{\"statusCode\":400,\"message\":\"error-username-invalid-character\",\"error\":\"HttpException\",\"path\":\"/api/v1/auth/register\"}";

            Assert.AreEqual(RegisterErrorReason.UsernameInvalid, RegisterErrorMapper.Map(400, body));
        }

        [Test]
        public void Map_KeycloakPasswordPolicy_ReturnsPasswordPolicy()
        {
            string body = "{\"statusCode\":400,\"message\":\"invalidPasswordMinLengthMessage\",\"details\":{\"error\":\"invalidPasswordMinLengthMessage\",\"error_description\":\"Invalid password: minimum length 8.\"}}";

            Assert.AreEqual(RegisterErrorReason.PasswordPolicy, RegisterErrorMapper.Map(400, body));
        }

        [Test]
        public void Map_ValidationEmailError_ReturnsEmailInvalid()
        {
            string body = "{\"statusCode\":400,\"message\":\"Validation failed\",\"error\":\"VALIDATION_ERROR\",\"details\":{\"errors\":[\"email: email must be an email\"]}}";

            Assert.AreEqual(RegisterErrorReason.EmailInvalid, RegisterErrorMapper.Map(400, body));
        }

        [Test]
        public void Map_KeycloakDuplicateEmail_ReturnsEmailTaken()
        {
            string body = "{\"statusCode\":409,\"message\":\"User exists with same email\"}";

            Assert.AreEqual(RegisterErrorReason.EmailTaken, RegisterErrorMapper.Map(409, body));
        }

        [Test]
        public void Map_KeycloakDuplicateUsername_ReturnsUsernameTaken()
        {
            string body = "{\"statusCode\":409,\"message\":\"User exists with same username\"}";

            Assert.AreEqual(RegisterErrorReason.UsernameTaken, RegisterErrorMapper.Map(409, body));
        }

        [Test]
        public void Map_UnknownErrors_ReturnNone()
        {
            Assert.AreEqual(RegisterErrorReason.None, RegisterErrorMapper.Map(400, "{\"statusCode\":400,\"message\":\"Invalid input data\"}"));
            Assert.AreEqual(RegisterErrorReason.None, RegisterErrorMapper.Map(409, "{\"statusCode\":409,\"message\":\"Conflict\"}"));
            Assert.AreEqual(RegisterErrorReason.None, RegisterErrorMapper.Map(500, "{\"message\":\"error-username-invalid-character\"}"));
            Assert.AreEqual(RegisterErrorReason.None, RegisterErrorMapper.Map(400, null));
        }

        [Test]
        public void GetLocalizationKey_ReturnsKeyPerReason()
        {
            Assert.AreEqual("ERROR_USERNAME_INVALID", RegisterErrorMapper.GetLocalizationKey(RegisterErrorReason.UsernameInvalid));
            Assert.AreEqual("API_ERROR_REGISTER_USERNAME_TAKEN", RegisterErrorMapper.GetLocalizationKey(RegisterErrorReason.UsernameTaken));
            Assert.AreEqual("ERROR_EMAIL_INVALID", RegisterErrorMapper.GetLocalizationKey(RegisterErrorReason.EmailInvalid));
            Assert.AreEqual("API_ERROR_REGISTER_EMAIL_TAKEN", RegisterErrorMapper.GetLocalizationKey(RegisterErrorReason.EmailTaken));
            Assert.AreEqual("API_ERROR_REGISTER_PASSWORD_POLICY", RegisterErrorMapper.GetLocalizationKey(RegisterErrorReason.PasswordPolicy));
            Assert.IsNull(RegisterErrorMapper.GetLocalizationKey(RegisterErrorReason.None));
        }
    }
}
