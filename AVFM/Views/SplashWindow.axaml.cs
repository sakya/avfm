using System.Reflection;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Media;

namespace AVFM.Views;

public partial class SplashWindow : Window
{
    public SplashWindow()
    {
        InitializeComponent();

        RenderOptions.SetBitmapInterpolationMode(ImageControl, BitmapInterpolationMode.HighQuality);
        TitleControl.Text = $"AVFM v.{Assembly.GetEntryAssembly()?.GetName().Version}";
        CopyrightControl.Text = ((AssemblyCopyrightAttribute?)System.Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(), typeof(AssemblyCopyrightAttribute), false))?.Copyright;
    }

    public void SetMessage(string message)
    {
        MessageTextControl.Text = message;
    }
}