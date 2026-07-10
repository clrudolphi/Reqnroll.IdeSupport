using System;
using System.Linq;
using System.Windows;

namespace Reqnroll.IdeSupport.VisualStudio.Wizards.UI.Infrastructure;

// source: https://dzone.com/articles/goodbye
/// <summary>
///     Definition of alternative attached properties for various
///     controls.
/// </summary>
public static class Alt
{
    public static readonly DependencyProperty IsVisibleProperty = DependencyProperty.RegisterAttached(
        "IsVisible", typeof(bool?), typeof(Alt), new PropertyMetadata(default(bool?), IsVisibleChangedCallback));

    private static void IsVisibleChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var fe = d as FrameworkElement;
        if (fe == null)
            return;

        fe.Visibility = (bool?) e.NewValue == true
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    /// <summary>Sets the attached <see cref="IsVisibleProperty"/> value on the given element.</summary>
    public static void SetIsVisible(DependencyObject element, bool? value)
    {
        element.SetValue(IsVisibleProperty, value);
    }

    /// <summary>Gets the attached <see cref="IsVisibleProperty"/> value from the given element.</summary>
    public static bool? GetIsVisible(DependencyObject element) => (bool?) element.GetValue(IsVisibleProperty);
}
