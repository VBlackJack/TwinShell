using System.Windows.Controls;
using TwinShell.App.ViewModels;

namespace TwinShell.App.Views;

/// <summary>
/// Interaction logic for PowerShellGalleryPanel.xaml
/// </summary>
public partial class PowerShellGalleryPanel : UserControl
{
    public PowerShellGalleryPanel(PowerShellGalleryViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
