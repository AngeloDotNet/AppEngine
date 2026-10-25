using System.ComponentModel.DataAnnotations;
using AppEngine.Extensions;

namespace AppEngine.Tests.Extensions;

public class EnumExtensionsTests
{
    [Flags]
    public enum Permission
    {
        None = 0,
        [Display(Name = "Can read")] Read = 1,
        Write = 2,
        Execute = 4,
        All = 7
    }

    public enum Color
    {
        Red = 1,
        [Display(Name = "Deep blue")] Blue = 2,
        Green = 3
    }

    public enum Aliased
    {
        First = 1,
        Second = 1
    }

    public enum SmallEnum : byte
    {
        A = 1,
        B = 2
    }

    [Fact]
    public void GetDescription_SingleValue_NoAttribute_UsesName() =>
        Assert.Equal("Red", Color.Red.GetDescription());

    [Fact]
    public void GetDescription_UsesDisplayName() =>
        Assert.Equal("Deep blue", Color.Blue.GetDescription());

    [Fact]
    public void GetDescription_Flags_JoinsDescriptions() =>
        Assert.Equal("Can read, Write", (Permission.Read | Permission.Write).GetDescription());

    [Fact]
    public void GetDescription_CombinedValueMatchingNamedMember_UsesThatMember() =>
        Assert.Equal("All", Permission.All.GetDescription());

    [Fact]
    public void GetDescription_ZeroFlag_UsesNoneName() =>
        Assert.Equal("None", Permission.None.GetDescription());

    [Fact]
    public void GetDescription_UndefinedValue_FallsBackToNumber() =>
        Assert.Equal("99", ((Color)99).GetDescription());

    [Fact]
    public void GetFlags_ReturnsSetFlagsInAscendingOrder() =>
        Assert.Equal([Permission.Read, Permission.Write], (Permission.Read | Permission.Write).GetFlags());

    [Fact]
    public void GetFlags_Zero_ReturnsNone() =>
        Assert.Equal([Permission.None], Permission.None.GetFlags());

    [Fact]
    public void GetFlags_NamedCombination_ReturnsOnlyThatMember() =>
        Assert.Equal([Permission.All], Permission.All.GetFlags());

    [Fact]
    public void GetFlags_UndefinedBits_ReturnsEmpty() =>
        Assert.Empty((Permission)8 is var value ? value.GetFlags() : []);

    [Fact]
    public void GetDescriptions_Generic_ReturnsAllMembers()
    {
        var descriptions = EnumExtensions.GetDescriptions<Color>();

        Assert.Equal(3, descriptions.Count);
        Assert.Equal("Red", descriptions[1]);
        Assert.Equal("Deep blue", descriptions[2]);
        Assert.Equal("Green", descriptions[3]);
    }

    [Fact]
    public void GetDescriptions_ByType_ReturnsAllMembers()
    {
        var descriptions = typeof(Permission).GetDescriptions();

        Assert.Equal("Can read", descriptions[1]);
        Assert.Equal("None", descriptions[0]);
    }

    [Fact]
    public void Current_GetDescriptions_NonIntBackedEnum_Throws()
    {
        // Documents current behavior: the underlying value is cast with (int) on a boxed byte.
        Assert.Throws<InvalidCastException>(() => EnumExtensions.GetDescriptions<SmallEnum>());
    }

    [Fact]
    public void Current_GetDescriptions_DuplicateValues_Throw()
    {
        // Documents current behavior: enum members that share a value
        // make Dictionary.Add throw.
        Assert.Throws<ArgumentException>(() => EnumExtensions.GetDescriptions<Aliased>());
    }
}
