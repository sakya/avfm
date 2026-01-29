using System;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using AVFM.Views;
using AVFM.Utils;
using Avalonia.Platform;

namespace AVFM
{
    public class App : Application
    {
        public static FileManagers.FsFileManager DefaultFsFileManager { get; set; }
        public static string LocalPath { get; set; }
        public static string SettingsPath { get; set; }

        public Settings Settings { get; set; }

        public override void Initialize()
        {
            // Load settings
            App.LocalPath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AVFM");
            if (!Directory.Exists(App.LocalPath))
                Directory.CreateDirectory(App.LocalPath);
            App.SettingsPath = Path.Join(App.LocalPath, "settings.json");
            if (File.Exists(App.SettingsPath)) {
                try {
                    Settings = Settings.Load(App.SettingsPath);
                } catch {
                    Settings = new Settings();                    
                }
            } else {
                Settings = new Settings();
                Settings.Save(App.SettingsPath);
            }

            Localizer.Localizer.Instance.LoadLanguage(Settings.Language);

            if (Environment.OSVersion.Platform == PlatformID.Unix)
                DefaultFsFileManager = new FileManagers.FsFileManagerLinux();
            else if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                DefaultFsFileManager = new FileManagers.FsFileManagerWindows();
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow();
            }

            base.OnFrameworkInitializationCompleted();
        }

        public static void SetWindowTitle(Avalonia.Controls.Window window)
        {
            // https://github.com/AvaloniaUI/Avalonia/issues/5632
            if (Environment.OSVersion.Platform == PlatformID.Win32NT) {
                var size = new Size(window.Width, window.Height);
                var sizeToContent = window.SizeToContent;

                window.ExtendClientAreaToDecorationsHint = true;
                window.ExtendClientAreaTitleBarHeightHint = -1;
                window.ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.NoChrome;

                window.Width = size.Width;
                window.Height = size.Height;
                window.SizeToContent = sizeToContent;
            }

            string title = Localizer.Localizer.Instance[$"WT_{window.GetType().Name}"];
            if (!string.IsNullOrEmpty(title))
                window.Title = $"AVFM - {title}";
        } // SetWindowTitle
    }
}
