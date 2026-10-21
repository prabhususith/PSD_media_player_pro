using System;
using System.IO;
using System.Windows;
using LibVLCSharp.Shared;

namespace OmniPlayer.UI
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                // Enable modern TLS protocols for HLS/M3U8 streams and web requests
                System.Net.ServicePointManager.SecurityProtocol = 
                    System.Net.SecurityProtocolType.Tls12 | 
                    System.Net.SecurityProtocolType.Tls13;
                System.Net.ServicePointManager.DefaultConnectionLimit = 64;
            }
            catch { }

            DispatcherUnhandledException += (sender, args) =>
            {
                try
                {
                    string logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OmniPlayer");
                    Directory.CreateDirectory(logDir);
                    File.AppendAllText(Path.Combine(logDir, "crash.log"), $"[{DateTime.Now}] Dispatcher Unhandled Exception: {args.Exception}\n\n");
                }
                catch { }

                MessageBox.Show($"An unexpected error occurred in Omni Player:\n\n{args.Exception.Message}\n\nStack: {args.Exception.StackTrace}",
                                "Omni Player Diagnostic Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                args.Handled = true;
            };

            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                try
                {
                    string logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OmniPlayer");
                    Directory.CreateDirectory(logDir);
                    File.AppendAllText(Path.Combine(logDir, "crash.log"), $"[{DateTime.Now}] AppDomain Unhandled Exception: {args.ExceptionObject}\n\n");
                }
                catch { }
            };

            base.OnStartup(e);

            try
            {
                string baseDir = AppContext.BaseDirectory;
                string vlcArch = Environment.Is64BitProcess ? "win-x64" : "win-x86";
                string[] candidateDirs = new[]
                {
                    Path.Combine(baseDir, "libvlc", vlcArch),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "libvlc", vlcArch),
                    Path.Combine(baseDir),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory),
                    @"C:\Program Files\VideoLAN\VLC"
                };

                bool initialized = false;
                foreach (var dir in candidateDirs)
                {
                    if (Directory.Exists(dir) && File.Exists(Path.Combine(dir, "libvlc.dll")))
                    {
                        LibVLCSharp.Shared.Core.Initialize(dir);
                        initialized = true;
                        break;
                    }
                }

                if (!initialized)
                {
                    LibVLCSharp.Shared.Core.Initialize();
                }
            }
            catch
            {
                // Fallback if libvlc native libraries are loaded from system path
            }
        }
    }
}
