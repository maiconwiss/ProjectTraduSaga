using System;
using System.Security.Cryptography;
using System.Text;

namespace TraduSaga.Infrastructure.Encryption
{
    public sealed class DpapiDataProtectionService : IDataProtectionService
    {
        public byte[] Protect(byte[] plaintext, string purpose)
        {
            if (plaintext == null)
            {
                throw new ArgumentNullException(nameof(plaintext));
            }

            return ProtectedData.Protect(plaintext, GetEntropy(purpose), DataProtectionScope.CurrentUser);
        }

        public byte[] Unprotect(byte[] protectedData, string purpose)
        {
            if (protectedData == null)
            {
                throw new ArgumentNullException(nameof(protectedData));
            }

            return ProtectedData.Unprotect(protectedData, GetEntropy(purpose), DataProtectionScope.CurrentUser);
        }

        private static byte[] GetEntropy(string purpose)
        {
            if (string.IsNullOrWhiteSpace(purpose))
            {
                throw new ArgumentException("A protection purpose is required.", nameof(purpose));
            }

            return Encoding.UTF8.GetBytes(purpose);
        }
    }
}
