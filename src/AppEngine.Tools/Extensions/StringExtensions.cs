using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace AppEngine.Tools.Extensions;

public static class StringExtensions
{
    extension(string? a)
    {
        public bool EqualsIgnoreCase(string? b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
    }

    extension(string? input)
    {
        public bool StartsWithIgnoreCase(string value) => input?.StartsWith(value, StringComparison.OrdinalIgnoreCase) ?? false;

        public bool EndsWithIgnoreCase(string value) => input?.EndsWith(value, StringComparison.OrdinalIgnoreCase) ?? false;

        public bool ContainsIgnoreCase(string value) => input?.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;

        [return: NotNullIfNotNull(nameof(input))]
        public string? GetValueOrDefault() => input.GetValueOrDefault(defaultValue: default, whiteSpaceAsEmpty: true);

        [return: NotNullIfNotNull(nameof(input))]
        [return: NotNullIfNotNull(nameof(defaultValue))]
        public string? GetValueOrDefault(string? defaultValue) => input.GetValueOrDefault(defaultValue, whiteSpaceAsEmpty: true);

        [return: NotNullIfNotNull(nameof(input))]
        [return: NotNullIfNotNull(nameof(defaultValue))]
        public string? GetValueOrDefault(string? defaultValue, bool whiteSpaceAsEmpty)
            => whiteSpaceAsEmpty ? (string.IsNullOrWhiteSpace(input) ? defaultValue : input) : (string.IsNullOrEmpty(input) ? defaultValue : input);
    }

    extension(string input)
    {
        public string ReplaceIgnoreCase(string pattern, string replacement)
            => Regex.Replace(input, Regex.Escape(pattern), replacement, RegexOptions.IgnoreCase);

        public string FirstCharToUpper()
        {
            var result = input switch
            {
                null or "" => string.Empty,
                _ => string.Concat(input[0].ToString().ToUpper(), input.AsSpan(1))
            };

            return result;
        }
    }

    extension([NotNullWhen(true)] string? input)
    {
        public bool HasValue()
            => input.HasValue(allowEmptyString: false, whiteSpaceAsEmpty: true);

        public bool HasValue(bool allowEmptyString)
            => input.HasValue(allowEmptyString, whiteSpaceAsEmpty: true);

        public bool HasValue(bool allowEmptyString, bool whiteSpaceAsEmpty)
            => allowEmptyString ? input is not null : whiteSpaceAsEmpty ? !string.IsNullOrWhiteSpace(input) : !string.IsNullOrEmpty(input);
    }
}