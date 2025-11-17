using System.Windows;
using TwinShell.App.ViewModels;

namespace TwinShell.App.Views;

/// <summary>
/// Window for creating and editing actions
/// </summary>
public partial class ActionEditorWindow : Window
{
    private readonly ActionEditorViewModel _viewModel;

    public ActionEditorWindow(ActionEditorViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        // Subscribe to dialog result changes
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ActionEditorViewModel.DialogResult))
        {
            if (_viewModel.DialogResult.HasValue)
            {
                DialogResult = _viewModel.DialogResult.Value;
                Close();
            }
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
        base.OnClosed(e);
    }
}
