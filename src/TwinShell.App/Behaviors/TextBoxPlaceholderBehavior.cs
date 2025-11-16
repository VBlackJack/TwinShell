using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace TwinShell.App.Behaviors;

/// <summary>
/// Attached property behavior to add placeholder text to TextBox controls using VisualBrush
/// </summary>
public static class TextBoxPlaceholderBehavior
{
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

        textBox.Background = visualBrush;

        // Hide placeholder when textbox has text
        textBox.TextChanged += (s, args) =>
        {
            if (s is not TextBox tb) return;

            if (string.IsNullOrEmpty(tb.Text))
            {
                tb.Background = visualBrush;
            }
            else
            {
                tb.Background = (Brush)Application.Current.Resources["SurfaceBrush"];
            }
        };

        // Update background based on initial text
        if (!string.IsNullOrEmpty(textBox.Text))
        {
            textBox.Background = (Brush)Application.Current.Resources["SurfaceBrush"];
        }
    }
}
