using CommunityToolkit.Mvvm.ComponentModel;

namespace AltecDriverSupport.App.ViewModels;

/// <summary>
/// Root view model. Placeholder until the first real feature (Phantom
/// Printer) gets its own view/view model and this becomes a shell that
/// hosts a nav pane between features (Phantom Printer, Driver Info, ...).
/// </summary>
public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = "Altec Driver Support";
}
