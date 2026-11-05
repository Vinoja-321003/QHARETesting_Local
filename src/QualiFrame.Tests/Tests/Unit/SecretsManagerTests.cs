using NUnit.Framework;
using QualiFrame.Framework.Security;
using System;

namespace QualiFrame.Tests.Tests.Unit
{
    /// <summary>
    /// Unit tests for the SecretsManager class.
    /// </summary>
    [TestFixture]
    public class SecretsManagerTests
    {
        private SecretsManager? _secretsManager;

        [SetUp]
        public void SetUp()
        {
            _secretsManager = SecretsManager.Instance;
        }

        [Test]
        public void Encrypt_WithValidString_ShouldReturnEncryptedString()
        {
            // Arrange
            var plainText = "test-secret";

            // Act
            var encrypted = _secretsManager!.Encrypt(plainText);

            // Assert
            Assert.That(encrypted, Is.Not.Null);
            Assert.That(encrypted, Is.Not.EqualTo(plainText));
            Assert.That(encrypted.Length, Is.GreaterThan(0));
        }

        [Test]
        public void Decrypt_WithEncryptedString_ShouldReturnOriginalString()
        {
            // Arrange
            var plainText = "test-secret";
            var encrypted = _secretsManager!.Encrypt(plainText);

            // Act
            var decrypted = _secretsManager.Decrypt(encrypted);

            // Assert
            Assert.That(decrypted, Is.EqualTo(plainText));
        }

        [Test]
        public void Encrypt_WithEmptyString_ShouldReturnEncryptedString()
        {
            // Arrange
            var plainText = string.Empty;

            // Act
            var encrypted = _secretsManager!.Encrypt(plainText);

            // Assert
            Assert.That(encrypted, Is.Not.Null);
        }

        [Test]
        public void Decrypt_WithInvalidEncryptedString_ShouldThrowException()
        {
            // Arrange
            var invalidEncrypted = "invalid-encrypted-string";

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>((Action)(() => _secretsManager!.Decrypt(invalidEncrypted)));
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception.Message, Does.Contain("Failed to decrypt value"));
        }

        [Test]
        public void GetSecret_WithNonExistentKey_ShouldThrowException()
        {
            // Arrange
            var nonExistentKey = "NonExistentTestKey";

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>((Action)(() => _secretsManager!.GetSecret(nonExistentKey)));
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception.Message, Does.Contain($"Secret '{nonExistentKey}' not found"));
        }
    }
}

