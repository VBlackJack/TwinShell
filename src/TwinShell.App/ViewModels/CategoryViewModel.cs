using CommunityToolkit.Mvvm.ComponentModel;
using TwinShell.Core.Models;

namespace TwinShell.App.ViewModels;

/// <summary>
/// ViewModel for displaying and editing a single category.
/// </summary>
public partial class CategoryViewModel : ObservableObject
{
    private readonly CustomCategory _category;

    [ObservableProperty]
    private string _id;

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private string _iconKey;

    [ObservableProperty]
    private string _colorHex;

    [ObservableProperty]
    private bool _isSystemCategory;

    [ObservableProperty]
    private bool _isHidden;

    [ObservableProperty]
    private string? _description;

    [ObservableProperty]
    private int _actionCount;

    public CategoryViewModel(CustomCategory category)
    {
        _category = category;
        _id = category.Id;
        _name = category.Name;
        _iconKey = category.IconKey;
        _colorHex = category.ColorHex;
        _isSystemCategory = category.IsSystemCategory;
        _isHidden = category.IsHidden;
        _description = category.Description;
        _actionCount = category.ActionIds.Count;
    }

    /// <summary>
    /// Creates an updated CustomCategory model from the current ViewModel state.
    /// </summary>
    public CustomCategory ToModel()
    {
        return new CustomCategory
        {
            Id = _category.Id,
            Name = Name,
            IconKey = IconKey,
            ColorHex = ColorHex,
            IsSystemCategory = IsSystemCategory,
            IsHidden = IsHidden,
            Description = Description,
            DisplayOrder = _category.DisplayOrder,
            CreatedAt = _category.CreatedAt,
            ModifiedAt = DateTime.UtcNow,
            ActionIds = _category.ActionIds
        };
    }
}
