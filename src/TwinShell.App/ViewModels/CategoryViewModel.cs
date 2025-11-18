using CommunityToolkit.Mvvm.ComponentModel;

namespace TwinShell.App.ViewModels;

/// <summary>
/// ViewModel for displaying and editing a single category.
/// </summary>
public partial class CategoryViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private string _originalName;

    [ObservableProperty]
    private int _actionCount;

    public CategoryViewModel(string categoryName, int actionCount)
    {
        _name = categoryName;
        _originalName = categoryName;
        _actionCount = actionCount;
    }
}
