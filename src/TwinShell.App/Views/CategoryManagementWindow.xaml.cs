using System.Windows;
using TwinShell.App.ViewModels;

namespace TwinShell.App.Views;

/// <summary>
/// Window for managing custom categories.
/// </summary>
public partial class CategoryManagementWindow : Window
{
    public CategoryManagementWindow(CategoryManagementViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += async (s, e) => await viewModel.InitializeAsync();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
