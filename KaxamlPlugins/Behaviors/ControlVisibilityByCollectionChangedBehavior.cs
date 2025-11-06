using System.Collections;
using System.Collections.Specialized;
using System.Windows;
using Microsoft.Xaml.Behaviors;

namespace KaxamlPlugins.Behaviors;

/// <summary>
/// Allows for a control's Visibility to be set based on a bound Collection having content.
/// </summary>
public sealed class ControlVisibilityByCollectionChangedBehavior : Behavior<FrameworkElement>
{
    /// <inheritdoc cref="IsInverse"/>
    public static readonly DependencyProperty IsInverseProperty = DependencyProperty.Register(
        nameof(IsInverse),
        typeof(bool),
        typeof(ControlVisibilityByCollectionChangedBehavior),
        new PropertyMetadata(default(bool)));

    /// <inheritdoc cref="SourceCollection"/>
    public static readonly DependencyProperty SourceCollectionProperty = DependencyProperty.Register(
        nameof(SourceCollection),
        typeof(IEnumerable),
        typeof(ControlVisibilityByCollectionChangedBehavior),
        new PropertyMetadata(default(IEnumerable?), SourceCollectionPropertyChangedCallback));

    /// <summary>
    /// Indicates if the visibility should be applied inverted, e.g. null/empty makes control Visible.
    /// </summary>
    public bool IsInverse
    {
        get => (bool)GetValue(IsInverseProperty);
        set => SetValue(IsInverseProperty, value);
    }

    /// <summary>
    /// Source collection that controls visibility.
    /// </summary>
    public IEnumerable? SourceCollection
    {
        get => (IEnumerable?)GetValue(SourceCollectionProperty);
        set => SetValue(SourceCollectionProperty, value);
    }

    private static void SourceCollectionPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var b = (ControlVisibilityByCollectionChangedBehavior)d;

        if (e.OldValue is INotifyCollectionChanged oldCollection)
            oldCollection.CollectionChanged -= b.SourceCollection_OnCollectionChanged;

        if (e.NewValue is INotifyCollectionChanged newCollection)
            newCollection.CollectionChanged += b.SourceCollection_OnCollectionChanged;
    }

    private void SourceCollection_OnCollectionChanged(object? _, NotifyCollectionChangedEventArgs __)
    {
        var enumerator = SourceCollection?.GetEnumerator();
        using var ___ = enumerator as IDisposable;

        AssociatedObject.Visibility = enumerator?.MoveNext() is true
            ? IsInverse
                ? Visibility.Collapsed
                : Visibility.Visible
            : IsInverse
                ? Visibility.Visible
                : Visibility.Collapsed;
    }
}