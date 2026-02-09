using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace AppEngine.Extensions;

public static class StringExtensions
{
    public static bool EqualsIgnoreCase(this string? a, string? b)
        => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    public static bool StartsWithIgnoreCase(this string? input, string value)
        => input?.StartsWith(value, StringComparison.OrdinalIgnoreCase) ?? false;

    public static bool EndsWithIgnoreCase(this string? input, string value)
        => input?.EndsWith(value, StringComparison.OrdinalIgnoreCase) ?? false;

    public static bool ContainsIgnoreCase(this string? input, string value)
        => input?.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;

    public static string ReplaceIgnoreCase(this string input, string pattern, string replacement)
        => Regex.Replace(input, Regex.Escape(pattern), replacement, RegexOptions.IgnoreCase);

    [return: NotNullIfNotNull(nameof(input))]
    public static string? GetValueOrDefault(this string? input)
        => input.GetValueOrDefault(defaultValue: default, whiteSpaceAsEmpty: true);

    [return: NotNullIfNotNull(nameof(input))]
    [return: NotNullIfNotNull(nameof(defaultValue))]
    public static string? GetValueOrDefault(this string? input, string? defaultValue)
        => input.GetValueOrDefault(defaultValue, whiteSpaceAsEmpty: true);

    [return: NotNullIfNotNull(nameof(input))]
    [return: NotNullIfNotNull(nameof(defaultValue))]
    public static string? GetValueOrDefault(this string? input, string? defaultValue, bool whiteSpaceAsEmpty)
        => whiteSpaceAsEmpty ? (string.IsNullOrWhiteSpace(input) ? defaultValue : input) : (string.IsNullOrEmpty(input) ? defaultValue : input);

    public static bool HasValue([NotNullWhen(true)] this string? input)
        => input.HasValue(allowEmptyString: false, whiteSpaceAsEmpty: true);

    public static bool HasValue([NotNullWhen(true)] this string? input, bool allowEmptyString)
        => input.HasValue(allowEmptyString, whiteSpaceAsEmpty: true);

    public static bool HasValue([NotNullWhen(true)] this string? input, bool allowEmptyString, bool whiteSpaceAsEmpty)
        => allowEmptyString ? input is not null : whiteSpaceAsEmpty ? !string.IsNullOrWhiteSpace(input) : !string.IsNullOrEmpty(input);

    public static string FirstCharToUpper(this string input)
    {
        var result = input switch
        {
            null or "" => string.Empty,
            _ => string.Concat(input[0].ToString().ToUpper(), input.AsSpan(1))
        };

        return result;
    }
}