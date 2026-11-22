using System;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;

namespace TraduSaga.Infrastructure.Encryption
{
    public static class SecureFileStorage
    {
        public static void EnsurePrivateDirectory(string directoryPath)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                throw new ArgumentException("A directory path is required.", nameof(directoryPath));
            }

            var fullPath = Path.GetFullPath(directoryPath);
            Directory.CreateDirectory(fullPath);

            var currentUser = WindowsIdentity.GetCurrent().User;
            if (currentUser == null)
            {
                throw new InvalidOperationException("The current Windows user SID is unavailable.");
            }

            var security = new DirectorySecurity();
            security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
            AddFullControlRule(security, currentUser);
            AddFullControlRule(security, new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null));
            AddFullControlRule(security, new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null));
            new DirectoryInfo(fullPath).SetAccessControl(security);
        }

        public static void WriteAtomically(string filePath, byte[] contents)
        {
            if (contents == null)
            {
                throw new ArgumentNullException(nameof(contents));
            }

            var fullPath = Path.GetFullPath(filePath);
            var directoryPath = Path.GetDirectoryName(fullPath);
            if (directoryPath == null)
            {
                throw new ArgumentException("The file path must have a parent directory.", nameof(filePath));
            }

            EnsurePrivateDirectory(directoryPath);

            var temporaryPath = Path.Combine(directoryPath, "." + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                using (var stream = new FileStream(
                    temporaryPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    4096,
                    FileOptions.WriteThrough))
                {
                    stream.Write(contents, 0, contents.Length);
                    stream.Flush(flushToDisk: true);
                }

                File.Move(temporaryPath, fullPath, overwrite: true);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
        }

        private static void AddFullControlRule(DirectorySecurity security, SecurityIdentifier identity)
        {
            var rule = new FileSystemAccessRule(
                identity,
                FileSystemRights.FullControl,
                InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                PropagationFlags.None,
                AccessControlType.Allow);
            security.AddAccessRule(rule);
        }
    }
}
