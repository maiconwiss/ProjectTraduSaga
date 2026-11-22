using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace TraduSaga.Infrastructure.Constants
{
    public static class Global
    {
        public static string AppPath;

        public static string ModelsPath;

        static Global()
        {
#if DEBUG
            AppPath = System.AppDomain.CurrentDomain.BaseDirectory;
#else
            AppPath = Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
#endif
            ModelsPath = Path.Combine(AppPath, "models");
        }


        public static Version GetVersion()
        {
            return new Version(FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly().Location).ProductVersion);
        }
    }
}
