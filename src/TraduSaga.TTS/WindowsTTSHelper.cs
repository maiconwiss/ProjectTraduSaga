using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Speech.Synthesis;
using TraduSaga.Infrastructure.Language;

namespace TraduSaga.TTS
{
    public static class WindowsTTSHelper
    {
        internal static Func<ProcessStartInfo, Process> CapabilityProcessStarter { get; set; } =
            startInfo => Process.Start(startInfo)
                ?? throw new InvalidOperationException("The TTS capability process could not be started.");

        public static async Task<bool> InstallTTSLanguageCapability(string languageCode)
        {
            var startInfo = CreateTTSCapabilityInstallStartInfo(languageCode);
            if (startInfo == null)
                throw new ArgumentException("The TTS language capability is not supported.", nameof(languageCode));

            using var process = CapabilityProcessStarter(startInfo);

            await process.WaitForExitAsync();

            return process.ExitCode == 0;
        }

        public static bool IsLanguageTTSCapabilityInstalled(string languageTag, bool exactMatching = false)
        {
            using var synth = new SpeechSynthesizer();

            try
            {
                // Try exact match
                var voices = synth.GetInstalledVoices(new CultureInfo(languageTag));
                if (voices.Count != 0)
                    return true;
            }
            catch
            {
                // Culture not valid — ignore
            }

            if (!exactMatching)
            {
                try
                {
                    // Fallback: check only the two-letter language code
                    var shortTag = languageTag.Split('-')[0];
                    var voices = synth.GetInstalledVoices(new CultureInfo(shortTag));
                    if (voices.Count != 0)
                        return true;
                }
                catch
                {
                    // Still not valid — ignore
                }
            }

            return false;
        }

        internal static ProcessStartInfo CreateTTSCapabilityInstallStartInfo(string languageCode)
        {
            if (!new LanguageDescriptorFactory().GetAll().Any(descriptor =>
                    string.Equals(descriptor.Code, languageCode, StringComparison.Ordinal)))
            {
                return null;
            }

            const string psCommand = @"
            $languageTag = [string]$args[0];
            $env:TERM = 'xterm';
            $Host.UI.RawUI.WindowTitle = 'Installing TTS Language...';
            Write-Host ('Installing ' + $languageTag + ' TTS Language Capability. Please wait...') -ForegroundColor Yellow;
            Write-Host 'This can take up to 20 minutes. Please be patient.' -ForegroundColor Yellow;

            $capabilityPrefix = 'Language.TextToSpeech~~~' + $languageTag + '~';
            $cap = Get-WindowsCapability -Online | Where-Object { $_.Name -like ($capabilityPrefix + '*') } | Select-Object -First 1;

            if ($cap) {
                try {
                    Add-WindowsCapability -Online -Name $cap.Name -ErrorAction Stop;
                    Write-Host 'Installation complete!' -ForegroundColor Green;
                    Start-Sleep -Seconds 2;
                    exit 0;
                } catch {
                    Write-Host 'Installation failed. Please try again or use the manual method.' -ForegroundColor Red;
                    Write-Host $_ -ForegroundColor Red;
                    Start-Sleep -Seconds 60;
                    exit 1;
                }
            } else {
                Write-Host 'Capability not found. Try installing the language manually.' -ForegroundColor Red;
                Start-Sleep -Seconds 60;
                exit 1;
            }
            ";

            var startInfo = new ProcessStartInfo
            {
                FileName = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.System),
                    "WindowsPowerShell",
                    "v1.0",
                    "powershell.exe"),
                Verb = "runas",
                UseShellExecute = true
            };

            startInfo.ArgumentList.Add("-NoProfile");
            startInfo.ArgumentList.Add("-NonInteractive");
            startInfo.ArgumentList.Add("-Command");
            startInfo.ArgumentList.Add(psCommand);
            startInfo.ArgumentList.Add(languageCode);

            return startInfo;
        } 

        public static List<VoiceInfo> GetAvailableVoicesForLanguage(string languageTag)
        {
            using var synth = new SpeechSynthesizer();
            var result = new List<VoiceInfo>();
            
            try
            {
                var voices = synth.GetInstalledVoices(new CultureInfo(languageTag));
                if (voices.Count > 0)
                {
                    result.AddRange(voices.Select(v => v.VoiceInfo));
                    return result;
                }
            }
            catch
            {
                
            }

            try
            {
                var shortTag = languageTag.Split('-')[0];
                var voices = synth.GetInstalledVoices(new CultureInfo(shortTag));
                if (voices.Count > 0)
                {
                    result.AddRange(voices.Select(v => v.VoiceInfo));
                }
            }
            catch
            {

            }

            return result;
        }      
    }
}
