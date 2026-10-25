using AppEngine.Enums;
using AppEngine.Extensions;

namespace AppEngine.Tests.Extensions;

public class DateExtensionsTests
{
    private static TimeZoneInfo Tokyo => TimeZoneInfo.FindSystemTimeZoneById("Asia/Tokyo");

    private static TimeZoneInfo Rome => TimeZoneInfo.FindSystemTimeZoneById("Europe/Rome");

    [Fact]
    public void DateTime_ToDateOnly_And_ToTimeOnly()
    {
        var value = new DateTime(2025, 3, 4, 15, 30, 45);

        Assert.Equal(new DateOnly(2025, 3, 4), value.ToDateOnly());
        Assert.Equal(new TimeOnly(15, 30, 45), value.ToTimeOnly());
    }

    [Fact]
    public void TimeSpan_ToTimeOnly() =>
        Assert.Equal(new TimeOnly(1, 30), TimeSpan.FromMinutes(90).ToTimeOnly());

    [Fact]
    public void TimeSpan_ToTimeOnly_OutOfRange_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => TimeSpan.FromHours(25).ToTimeOnly());

    [Fact]
    public void DateOnly_ToDateTimeOffset_NoZone_IsUtcMidnight()
    {
        var result = new DateOnly(2025, 3, 4).ToDateTimeOffset();

        Assert.Equal(new DateTimeOffset(2025, 3, 4, 0, 0, 0, TimeSpan.Zero), result);
    }

    [Fact]
    public void DateOnly_ToDateTimeOffset_UsesZoneOffsetOfThatDate()
    {
        Assert.Equal(TimeSpan.FromHours(1), new DateOnly(2025, 1, 15).ToDateTimeOffset(Rome).Offset);
        Assert.Equal(TimeSpan.FromHours(2), new DateOnly(2025, 7, 15).ToDateTimeOffset(Rome).Offset);
    }

    [Fact]
    public void DateTimeOffset_ToDateOnly_ConvertsToZone()
    {
        var value = new DateTimeOffset(2025, 3, 4, 20, 0, 0, TimeSpan.Zero);

        Assert.Equal(new DateOnly(2025, 3, 4), value.ToDateOnly());
        Assert.Equal(new DateOnly(2025, 3, 5), value.ToDateOnly(Tokyo));
    }

    [Fact]
    public void DateTimeOffset_ToTimeOnly_NoZone_UsesLocalClockOfTheOffsetValue()
    {
        var value = new DateTimeOffset(2025, 3, 4, 10, 15, 0, TimeSpan.FromHours(2));

        Assert.Equal(new TimeOnly(10, 15), value.ToTimeOnly());
    }

    [Fact]
    public void Current_DateTimeOffset_ToTimeOnly_IgnoresZoneParameter()
    {
        // Documents current behavior (bug): the zone is converted but the result is built from the original value.
        // A correct implementation would return 19:00 for Tokyo.
        var value = new DateTimeOffset(2025, 3, 4, 10, 0, 0, TimeSpan.Zero);

        var result = value.ToTimeOnly(Tokyo);

        Assert.Equal(new TimeOnly(10, 0), result);
        Assert.NotEqual(new TimeOnly(19, 0), result);
    }
}

public class GuidExtensionsTests
{
    [Fact]
    public void Guid_EmptyChecks()
    {
        Assert.True(Guid.Empty.IsEmpty());
        Assert.False(Guid.Empty.IsNotEmpty());
        Assert.False(Guid.Empty.HasValue());

        var id = Guid.NewGuid();
        Assert.False(id.IsEmpty());
        Assert.True(id.IsNotEmpty());
        Assert.True(id.HasValue());
    }

    [Fact]
    public void Guid_GetValueOrCreateNew()
    {
        var id = Guid.NewGuid();

        Assert.Equal(id, id.GetValueOrCreateNew());
        Assert.NotEqual(Guid.Empty, Guid.Empty.GetValueOrCreateNew());
        Assert.Equal(7, Guid.Empty.GetValueOrCreateNew(GuidVersion.Version7).Version);
        Assert.Equal(4, Guid.Empty.GetValueOrCreateNew(GuidVersion.Version4).Version);
        Assert.Equal(id, id.GetValueOrCreateNew(GuidVersion.Version7));
    }

    [Fact]
    public void Guid_GetValueOrDefault()
    {
        var fallback = Guid.NewGuid();
        var id = Guid.NewGuid();

        Assert.Equal(fallback, Guid.Empty.GetValueOrDefault(fallback));
        Assert.Equal(id, id.GetValueOrDefault(fallback));
    }

    [Fact]
    public void NullableGuid_Checks()
    {
        Guid? nothing = null;
        Guid? empty = Guid.Empty;
        Guid? value = Guid.NewGuid();

        Assert.True(nothing.IsEmpty());
        Assert.True(empty.IsEmpty());
        Assert.False(value.IsEmpty());
        Assert.True(value.IsNotEmpty());
        Assert.False(nothing.IsNotEmpty());
        Assert.True(value.HasValue());
        Assert.False(nothing.HasValue());
    }

    [Fact]
    public void NullableGuid_GetValueOrCreateNew()
    {
        Guid? nothing = null;
        Guid? value = Guid.NewGuid();

        Assert.NotEqual(Guid.Empty, nothing.GetValueOrCreateNew());
        Assert.Equal(value.Value, value.GetValueOrCreateNew());
        Assert.Equal(7, nothing.GetValueOrCreateNew(GuidVersion.Version7).Version);
        Assert.Equal(value.Value, value.GetValueOrCreateNew(GuidVersion.Version7));
    }
}

public class ComparableExtensionsTests
{
    [Theory]
    [InlineData(BoundaryType.Inclusive, 1, true)]
    [InlineData(BoundaryType.Inclusive, 10, true)]
    [InlineData(BoundaryType.Inclusive, 5, true)]
    [InlineData(BoundaryType.Inclusive, 0, false)]
    [InlineData(BoundaryType.Inclusive, 11, false)]
    [InlineData(BoundaryType.LowerInclusive, 1, true)]
    [InlineData(BoundaryType.LowerInclusive, 10, false)]
    [InlineData(BoundaryType.UpperExclusive, 1, true)]
    [InlineData(BoundaryType.UpperExclusive, 10, false)]
    [InlineData(BoundaryType.UpperInclusive, 1, false)]
    [InlineData(BoundaryType.UpperInclusive, 10, true)]
    [InlineData(BoundaryType.LowerExclusive, 1, false)]
    [InlineData(BoundaryType.LowerExclusive, 10, true)]
    [InlineData(BoundaryType.Exclusive, 1, false)]
    [InlineData(BoundaryType.Exclusive, 10, false)]
    [InlineData(BoundaryType.Exclusive, 5, true)]
    public void IsBetween_Struct_RespectsBoundaryType(BoundaryType boundary, int value, bool expected) =>
        Assert.Equal(expected, value.IsBetween(1, 10, boundary));

    [Theory]
    [InlineData(BoundaryType.Inclusive, "a", true)]
    [InlineData(BoundaryType.Exclusive, "a", false)]
    [InlineData(BoundaryType.LowerInclusive, "c", false)]
    [InlineData(BoundaryType.UpperInclusive, "c", true)]
    public void IsBetween_ReferenceComparable_RespectsBoundaryType(BoundaryType boundary, string value, bool expected) =>
        Assert.Equal(expected, ((IComparable<string>)value).IsBetween("a", "c", boundary));

    [Fact]
    public void IsBetween_InvalidBoundaryType_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => 5.IsBetween(1, 10, (BoundaryType)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => ((IComparable<string>)"b").IsBetween("a", "c", (BoundaryType)99));
    }

    [Fact]
    public void IsBetween_CustomComparer_IsUsed()
    {
        var reverse = Comparer<int>.Create((x, y) => y.CompareTo(x));

        Assert.True(5.IsBetween(10, 1, BoundaryType.Inclusive, reverse));
        Assert.False(5.IsBetween(1, 10, BoundaryType.Inclusive, reverse));
    }
}
