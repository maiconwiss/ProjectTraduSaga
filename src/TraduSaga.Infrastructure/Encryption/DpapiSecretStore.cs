using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace TraduSaga.Infrastructure.Encryption
{
    public sealed class DpapiSecretStore : ISecretStore
    {
        public const string DeepLApiKeyName = "deepl-api-key";
        public const string YandexApiKeyName = "yandex-api-key";

        private readonly IDataProtectionService _dataProtectionService;
        private readonly string _directoryPath;

        public DpapiSecretStore(IDataProtectionService dataProtectionService, string directoryPath = null)
        {
            _dataProtectionService = dataProtectionService ?? throw new ArgumentNullException(nameof(dataProtectionService));

            _directoryPath = directoryPath ?? GetDefaultDirectoryPath();
        }

        public string GetSecret(string name)
        {
            var path = GetSecretPath(name);
            if (!File.Exists(path))
            {
                return null;
            }

            var protectedValue = File.ReadAllBytes(path);
            var plaintext = _dataProtectionService.Unprotect(protectedValue, GetPurpose(name));
            return new UTF8Encoding(false, true).GetString(plaintext);
        }

        public void SetSecret(string name, string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (value.Length == 0)
            {
                DeleteSecret(name);
                return;
            }

            var protectedValue = _dataProtectionService.Protect(Encoding.UTF8.GetBytes(value), GetPurpose(name));
            SecureFileStorage.WriteAtomically(GetSecretPath(name), protectedValue);
        }

        public void DeleteSecret(string name)
        {
            var path = GetSecretPath(name);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private string GetSecretPath(string name)
        {
            if (name != DeepLApiKeyName && name != YandexApiKeyName)
            {
                throw new ArgumentException("Unknown secret record.", nameof(name));
            }

            return Path.Combine(_directoryPath, name + ".bin");
        }

        internal static string GetDefaultDirectoryPath()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "TraduSaga",
                "Secrets");
        }
        internal static string GetPurpose(string name)
        {
            return "TraduSaga.Secret.v1." + name;
        }
    }
}
