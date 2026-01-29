using System.Threading.Tasks;
using Avalonia;
using Avalonia.Threading;
using AVFM.Views;
using Projektanker.Icons.Avalonia;
using Projektanker.Icons.Avalonia.FontAwesome;

namespace AVFM
{
    class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        public static void Main(string[] args)
        {
            BuildAvaloniaApp()
                .Start(AppMain, args);
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp() {
            IconProvider.Current
                .Register<FontAwesomeIconProvider>();

            return AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace();
        }

        private static void AppMain(Application app, string[] args)
        {
            var mApp = (App)app;
            async void Start()
            {
                var splash = new SplashWindow();
                splash.Show();
                await Dispatcher.UIThread.InvokeAsync(
                    () => { },
                    DispatcherPriority.Render
                );

                if (!await Task.Run(() => InitializeApp(splash, args))) {
                    splash.Close();
                    await mApp.CancellationToken.CancelAsync();
                    return;
                }

                var w = new MainWindow();
                w.Closed += (_, _) =>
                {
                    mApp.CancellationToken.Cancel();
                };
                w.Show();
                w.Activate();
                splash.Close();
            }
            Start();
            mApp.Run();
        }

        private static async Task<bool> InitializeApp(SplashWindow splashWindow, string[] args)
        {
            return true;
        }
    }
}
