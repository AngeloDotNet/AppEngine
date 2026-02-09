using AppEngine.Enums;

namespace AppEngine.Extensions;

public static class IComparableExtensions
{
    public static bool IsBetween<T>(this T value, T lowerValue, T upperValue, BoundaryType boundaryType = BoundaryType.Inclusive, Comparer<T>? comparer = default) where T : struct, IComparable<T>
    {
        var valueComparer = comparer ?? Comparer<T>.Default;

        return boundaryType switch
        {
            BoundaryType.Inclusive => valueComparer.Compare(value, lowerValue) >= 0 && valueComparer.Compare(value, upperValue) <= 0,
            BoundaryType.LowerInclusive or BoundaryType.UpperExclusive => valueComparer.Compare(value, lowerValue) >= 0 && valueComparer.Compare(value, upperValue) < 0,
            BoundaryType.UpperInclusive or BoundaryType.LowerExclusive => valueComparer.Compare(value, lowerValue) > 0 && valueComparer.Compare(value, upperValue) <= 0,
            BoundaryType.Exclusive => valueComparer.Compare(value, lowerValue) > 0 && valueComparer.Compare(value, upperValue) < 0,
            _ => throw new ArgumentOutOfRangeException(nameof(boundaryType), boundaryType, "Invalid boundary type specified.")
        };
    }

    public static bool IsBetween<T>(this IComparable<T> value, T lowerValue, T upperValue, BoundaryType boundaryType = BoundaryType.Inclusive)
    {
        return boundaryType switch
        {
            BoundaryType.Inclusive => value.CompareTo(lowerValue) >= 0 && value.CompareTo(upperValue) <= 0,
            BoundaryType.LowerInclusive or BoundaryType.UpperExclusive => value.CompareTo(lowerValue) >= 0 && value.CompareTo(upperValue) < 0,
            BoundaryType.UpperInclusive or BoundaryType.LowerExclusive => value.CompareTo(lowerValue) > 0 && value.CompareTo(upperValue) <= 0,
            BoundaryType.Exclusive => value.CompareTo(lowerValue) > 0 && value.CompareTo(upperValue) < 0,
            _ => throw new ArgumentOutOfRangeException(nameof(boundaryType), boundaryType, "Invalid boundary type specified.")
        };
    }
}