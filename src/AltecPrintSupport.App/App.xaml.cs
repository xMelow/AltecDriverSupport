using System.Windows;
using Wpf.Ui.Appearance;

namespace AltecPrintSupport.App;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Follow the Windows light/dark setting instead of the static "Light"
        // dictionary merged in App.xaml (that one only covers design-time/XAML).
        ApplicationThemeManager.ApplySystemTheme();
    }
}
