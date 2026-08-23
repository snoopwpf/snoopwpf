namespace Snoop.Infrastructure;

using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

/// <summary>
/// Attached properties which describe how a <see cref="GridViewColumn"/> takes part in sorting.
/// <para />
/// The sort descriptions of the sorted view stay the single source of truth.
/// <see cref="UpdateSortIndicators"/> just mirrors them onto the columns so that
/// the column headers can display a sort indicator.
/// </summary>
public static class GridViewSort
{
    /// <summary>
    /// Identifies the "PropertyName" attached property.
    /// </summary>
    public static readonly DependencyProperty PropertyNameProperty =
        DependencyProperty.RegisterAttached(
            "PropertyName",
            typeof(string),
            typeof(GridViewSort),
            new PropertyMetadata(default(string)));

    /// <summary>Helper for getting <see cref="PropertyNameProperty"/> from <paramref name="element"/>.</summary>
    /// <param name="element"><see cref="GridViewColumn"/> to read <see cref="PropertyNameProperty"/> from.</param>
    /// <returns>PropertyName property value.</returns>
    [AttachedPropertyBrowsableForType(typeof(GridViewColumn))]
    public static string? GetPropertyName(GridViewColumn element)
    {
        return (string?)element.GetValue(PropertyNameProperty);
    }

    /// <summary>Helper for setting <see cref="PropertyNameProperty"/> on <paramref name="element"/>.</summary>
    /// <param name="element"><see cref="GridViewColumn"/> to set <see cref="PropertyNameProperty"/> on.</param>
    /// <param name="value">PropertyName property value.</param>
    public static void SetPropertyName(GridViewColumn element, string? value)
    {
        element.SetValue(PropertyNameProperty, value);
    }

    /// <summary>
    /// Identifies the "Direction" attached property.
    /// </summary>
    /// <remarks>
    /// This property is set on the <see cref="GridViewColumn"/> by <see cref="UpdateSortIndicators"/>
    /// and forwarded to the <see cref="GridViewColumnHeader"/> by its style,
    /// because the header template can only display the sort indicator for a property of the header itself.
    /// </remarks>
    public static readonly DependencyProperty DirectionProperty =
        DependencyProperty.RegisterAttached(
            "Direction",
            typeof(ListSortDirection?),
            typeof(GridViewSort),
            new PropertyMetadata(default(ListSortDirection?)));

    /// <summary>Helper for getting <see cref="DirectionProperty"/> from <paramref name="element"/>.</summary>
    /// <param name="element"><see cref="DependencyObject"/> to read <see cref="DirectionProperty"/> from.</param>
    /// <returns>Direction property value.</returns>
    [AttachedPropertyBrowsableForType(typeof(GridViewColumn))]
    public static ListSortDirection? GetDirection(DependencyObject element)
    {
        return (ListSortDirection?)element.GetValue(DirectionProperty);
    }

    /// <summary>Helper for setting <see cref="DirectionProperty"/> on <paramref name="element"/>.</summary>
    /// <param name="element"><see cref="DependencyObject"/> to set <see cref="DirectionProperty"/> on.</param>
    /// <param name="value">Direction property value.</param>
    public static void SetDirection(DependencyObject element, ListSortDirection? value)
    {
        element.SetValue(DirectionProperty, value);
    }

    /// <summary>
    /// Updates <see cref="DirectionProperty"/> on all columns of <paramref name="gridView"/> to reflect <paramref name="sortDescriptions"/>.
    /// </summary>
    public static void UpdateSortIndicators(GridView? gridView, SortDescriptionCollection? sortDescriptions)
    {
        if (gridView is null)
        {
            return;
        }

        foreach (var column in gridView.Columns)
        {
            SetDirection(column, GetSortDirection(column, sortDescriptions));
        }
    }

    private static ListSortDirection? GetSortDirection(GridViewColumn column, SortDescriptionCollection? sortDescriptions)
    {
        var propertyName = GetPropertyName(column);

        if (string.IsNullOrEmpty(propertyName)
            || sortDescriptions is null)
        {
            return null;
        }

        foreach (var sortDescription in sortDescriptions)
        {
            if (sortDescription.PropertyName == propertyName)
            {
                return sortDescription.Direction;
            }
        }

        return null;
    }
}
