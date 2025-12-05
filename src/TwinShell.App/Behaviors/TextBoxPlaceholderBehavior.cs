using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TwinShell.App.Behaviors;

/// <summary>
/// Attached property behavior to add placeholder text to TextBox controls using VisualBrush.
/// Uses WeakEventManager to prevent memory leaks from event handler subscriptions.
/// </summary>
public static class TextBoxPlaceholderBehavior
{
    // Store visual brushes per TextBox to avoid recreating them
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<TextBox, VisualBrush> _visualBrushes = new();

    public static readonly DependencyProperty PlaceholderProperty =
        DependencyProperty.RegisterAttached(
            "Placeholder",
            typeof(string),
            typeof(TextBoxPlaceholderBehavior),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsRender, OnPlaceholderChanged));

    public static string GetPlaceholder(DependencyObject obj)
    {
        return (string)obj.GetValue(PlaceholderProperty);
    }

    public static void SetPlaceholder(DependencyObject obj, string value)
    {
        obj.SetValue(PlaceholderProperty, value);
    }

    private static void OnPlaceholderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBox textBox)
            return;

        var placeholder = e.NewValue as string;
        if (string.IsNullOrEmpty(placeholder))
            return;

        var visualBrush = new VisualBrush
        {
            AlignmentX = AlignmentX.Left,
            AlignmentY = AlignmentY.Center,
            Stretch = Stretch.None,
            Visual = new TextBlock
            {
                Text = placeholder,
                Foreground = (Brush)Application.Current.Resources["TextSecondaryBrush"],
                FontSize = textBox.FontSize,
                Margin = new Thickness(2, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            }
        };

        // Store brush reference using weak table (allows GC when TextBox is collected)
        _visualBrushes.AddOrUpdate(textBox, visualBrush);

        textBox.Background = visualBrush;

        // BUGFIX: Use WeakEventManager to prevent memory leaks
        // This ensures the event handler doesn't prevent TextBox from being garbage collected
        WeakEventManager<TextBox, TextChangedEventArgs>.AddHandler(
            textBox,
            nameof(TextBox.TextChanged),
            OnTextChanged);

        // Update background based on initial text
        if (!string.IsNullOrEmpty(textBox.Text))
        {
            textBox.Background = (Brush)Application.Current.Resources["SurfaceBrush"];
        }
    }

    private static void OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox textBox)
            return;

        if (string.IsNullOrEmpty(textBox.Text))
        {
            // Restore placeholder visual brush
            if (_visualBrushes.TryGetValue(textBox, out var visualBrush))
            {
                textBox.Background = visualBrush;
            }
        }
        else
        {
            textBox.Background = (Brush)Application.Current.Resources["SurfaceBrush"];
        }
    }
}
