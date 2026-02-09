using System.Diagnostics.CodeAnalysis;
using AppEngine.Enums;

namespace AppEngine.Extensions;

public static class GuidExtensions
{
    public static bool IsEmpty(this Guid input) => input == Guid.Empty;

    public static bool IsNotEmpty(this Guid input) => !input.IsEmpty();

    public static bool IsEmpty([NotNullWhen(false)] this Guid? input) => input.GetValueOrDefault() == Guid.Empty;

    public static bool IsNotEmpty([NotNullWhen(true)] this Guid? input) => !input.IsEmpty();

    public static bool HasValue(this Guid input) => !input.IsEmpty();

    public static bool HasValue([NotNullWhen(true)] this Guid? input) => !input.IsEmpty();

    public static Guid GetValueOrCreateNew(this Guid input) => input.IsEmpty() ? Guid.NewGuid() : input;

    public static Guid GetValueOrDefault(this Guid input, Guid defaultValue) => input.IsEmpty() ? defaultValue : input;

    public static Guid GetValueOrCreateNew(this Guid? input) => input.IsEmpty() ? Guid.NewGuid() : input!.Value;

    public static Guid GetValueOrCreateNew(this Guid input, GuidVersion guidVersion)
        => input.IsEmpty() ? guidVersion switch
        {
            GuidVersion.Version7 => Guid.CreateVersion7(),
            _ => Guid.NewGuid()
        } : input;

    public static Guid GetValueOrCreateNew(this Guid? input, GuidVersion guidVersion)
        => input.IsEmpty() ? guidVersion switch
        {
            GuidVersion.Version7 => Guid.CreateVersion7(),
            _ => Guid.NewGuid()
        } : input!.Value;
}