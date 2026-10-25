using System.Globalization;
using AppEngine.Extensions;

namespace AppEngine.Tests.Extensions;

public class StringExtensionsTests
{
    [Theory]
    [InlineData("abc", "ABC", true)]
    [InlineData("abc", "abd", false)]
    [InlineData(null, null, true)]
    [InlineData("abc", null, false)]
    public void EqualsIgnoreCase(string? a, string? b, bool expected) =>
        Assert.Equal(expected, a.EqualsIgnoreCase(b));

    [Fact]
    public void StartsEndsContains_IgnoreCase()
    {
        Assert.True("Hello World".StartsWithIgnoreCase("hello"));
        Assert.False("Hello World".StartsWithIgnoreCase("world"));
        Assert.True("Hello World".EndsWithIgnoreCase("WORLD"));
        Assert.True("Hello World".ContainsIgnoreCase("O W"));
        Assert.False("Hello World".ContainsIgnoreCase("xyz"));
    }

    [Fact]
    public void StartsEndsContains_NullInput_ReturnFalse()
    {
        string? input = null;

        Assert.False(input.StartsWithIgnoreCase("a"));
        Assert.False(input.EndsWithIgnoreCase("a"));
        Assert.False(input.ContainsIgnoreCase("a"));
    }

    [Fact]
    public void ContainsIgnoreCase_EmptyValue_IsTrue() =>
        Assert.True("abc".ContainsIgnoreCase(string.Empty));

    [Theory]
    [InlineData("abc", "x", "abc")]
    [InlineData(null, "x", "x")]
    [InlineData("", "x", "x")]
    [InlineData("   ", "x", "x")]
    public void GetValueOrDefault_WhiteSpaceAsEmpty(string? input, string defaultValue, string expected) =>
        Assert.Equal(expected, input.GetValueOrDefault(defaultValue));

    [Fact]
    public void GetValueOrDefault_WhiteSpaceNotEmpty_KeepsWhitespace()
    {
        Assert.Equal("   ", "   ".GetValueOrDefault("x", whiteSpaceAsEmpty: false));
        Assert.Equal("x", "".GetValueOrDefault("x", whiteSpaceAsEmpty: false));
    }

    [Fact]
    public void GetValueOrDefault_NoDefault_ReturnsNullForBlank()
    {
        string? blank = "  ";

        Assert.Null(blank.GetValueOrDefault());
        Assert.Equal("a", "a".GetValueOrDefault());
    }

    [Theory]
    [InlineData("abc", true)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData(null, false)]
    public void HasValue_Default(string? input, bool expected) =>
        Assert.Equal(expected, input.HasValue());

    [Fact]
    public void HasValue_Options()
    {
        string? nothing = null;

        Assert.True("".HasValue(allowEmptyString: true));
        Assert.False(nothing.HasValue(allowEmptyString: true));
        Assert.True("  ".HasValue(allowEmptyString: false, whiteSpaceAsEmpty: false));
        Assert.False("".HasValue(allowEmptyString: false, whiteSpaceAsEmpty: false));
    }

    [Theory]
    [InlineData("Hello World", "world", "there", "Hello there")]
    [InlineData("aAaA", "a", "b", "bbbb")]
    [InlineData("a.b.c", ".", "-", "a-b-c")]
    [InlineData("price (usd)", "(USD)", "eur", "price eur")]
    [InlineData("nothing", "xyz", "q", "nothing")]
    public void ReplaceIgnoreCase_TreatsPatternAsLiteral(string input, string pattern, string replacement, string expected) =>
        Assert.Equal(expected, input.ReplaceIgnoreCase(pattern, replacement));

    [Fact]
    public void Current_ReplaceIgnoreCase_ReplacementIsInterpretedAsRegexSubstitution()
    {
        // Documents current behavior: the replacement goes through Regex.Replace, so "$&" is the matched text, not a literal.
        // Switching to string.Replace(..., OrdinalIgnoreCase) (Phase 4) would change this.
        Assert.Equal("abbc", "abc".ReplaceIgnoreCase("b", "$&$&"));
    }

    [Fact]
    public void ReplaceIgnoreCase_RegexMetacharactersInPattern_AreEscaped()
    {
        var evil = "(a+)+$";

        var result = new string('a', 50_000).ReplaceIgnoreCase(evil, "x");

        Assert.Equal(new string('a', 50_000), result);
    }

    [Theory]
    [InlineData("hello", "Hello")]
    [InlineData("Hello", "Hello")]
    [InlineData("h", "H")]
    [InlineData("1abc", "1abc")]
    [InlineData("", "")]
    public void FirstCharToUpper(string input, string expected) =>
        Assert.Equal(expected, input.FirstCharToUpper());

    [Fact]
    public void FirstCharToUpper_Null_ReturnsEmpty()
    {
        string input = null!;

        Assert.Equal(string.Empty, input.FirstCharToUpper());
    }

    [Fact]
    public void Current_FirstCharToUpper_IsCultureSensitive()
    {
        // Documents current behavior: ToUpper() uses the current culture, so Turkish "i" becomes "İ".
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo turkish;
            try
            {
                turkish = new CultureInfo("tr-TR");
            }
            catch (CultureNotFoundException)
            {
                return; // invariant globalization mode: nothing to verify
            }

            CultureInfo.CurrentCulture = turkish;
            var result = "id".FirstCharToUpper();

            Assert.Equal(turkish.TextInfo.ToUpper("i"), result[..1]);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }
}
