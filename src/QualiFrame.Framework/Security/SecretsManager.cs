using System;
using System.Security.Cryptography;
using System.Text;
using Azure.Security.KeyVault.Secrets;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using QualiFrame.Framework.Configuration;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Framework.Security
{
    /// <summary>
    /// Manages secrets and credentials securely.
    /// </summary>
    public class SecretsManager
    {
        private static SecretsManager? _instance;
        private static readonly object _lock = new object();
        private readonly TestConfiguration _config;
        private SecretClient? _keyVaultClient;
        private readonly bool _useKeyVault;

        private SecretsManager()
        {
            _config = TestConfiguration.Instance;
            _useKeyVault = !string.IsNullOrEmpty(_config.KeyVaultUrl);
            
            if (_useKeyVault)
            {
                try
                {
                    var credential = new DefaultAzureCredential();
                    _keyVaultClient = new SecretClient(new Uri(_config.KeyVaultUrl!), credential);
                    Logger.Information("Azure Key Vault client initialized");
                }
                catch (Exception ex)
                {
                    Logger.Warning("Failed to initialize Azure Key Vault client: {Message}. Falling back to local encryption.", ex.Message);
                    _useKeyVault = false;
                }
            }
        }

        /// <summary>
        /// Gets the singleton instance.
        /// </summary>
        public static SecretsManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new SecretsManager();
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Gets a secret value from Key Vault or encrypted configuration.
        /// </summary>
        public string GetSecret(string secretName)
        {
            if (_useKeyVault && _keyVaultClient != null)
            {
                try
                {
                    var secret = _keyVaultClient.GetSecret(secretName);
                    return secret.Value.Value;
                }
                catch (Exception ex)
                {
                    Logger.Warning("Failed to retrieve secret from Key Vault: {Message}", ex.Message);
                }
            }

            var encryptedValue = _config.GetEncryptedValue(secretName);
            if (!string.IsNullOrEmpty(encryptedValue))
            {
                return Decrypt(encryptedValue);
            }

            throw new InvalidOperationException($"Secret '{secretName}' not found");
        }

        /// <summary>
        /// Encrypts a value using AES encryption.
        /// </summary>
        public string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;

            var encryptionKey = _config.EncryptionKey;
            if (string.IsNullOrEmpty(encryptionKey) || encryptionKey == "YourEncryptionKeyHere")
            {
                Logger.Warning("Using default encryption key. For production, use a secure key.");
                encryptionKey = "DefaultEncryptionKey123456789012345678901234567890";
            }

            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(encryptionKey.PadRight(32).Substring(0, 32));
            aes.IV = new byte[16];

            using var encryptor = aes.CreateEncryptor();
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            return Convert.ToBase64String(encryptedBytes);
        }

        /// <summary>
        /// Decrypts a value using AES decryption.
        /// </summary>
        public string Decrypt(string encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText))
                return encryptedText;

            var encryptionKey = _config.EncryptionKey;
            if (string.IsNullOrEmpty(encryptionKey) || encryptionKey == "YourEncryptionKeyHere")
            {
                encryptionKey = "DefaultEncryptionKey123456789012345678901234567890";
            }

            try
            {
                using var aes = Aes.Create();
                aes.Key = Encoding.UTF8.GetBytes(encryptionKey.PadRight(32).Substring(0, 32));
                aes.IV = new byte[16];

                using var decryptor = aes.CreateDecryptor();
                var encryptedBytes = Convert.FromBase64String(encryptedText);
                var decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);

                return Encoding.UTF8.GetString(decryptedBytes);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to decrypt value");
                throw new InvalidOperationException("Failed to decrypt value", ex);
            }
        }

        /// <summary>
        /// Stores a secret in Key Vault.
        /// </summary>
        public void SetSecret(string secretName, string secretValue)
        {
            if (_useKeyVault && _keyVaultClient != null)
            {
                try
                {
                    _keyVaultClient.SetSecret(secretName, secretValue);
                    Logger.Information("Secret '{SecretName}' stored in Key Vault", secretName);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Failed to store secret in Key Vault");
                    throw;
                }
            }
            else
            {
                throw new InvalidOperationException("Key Vault is not configured");
            }
        }
    }
}
