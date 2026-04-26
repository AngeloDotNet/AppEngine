using System.Diagnostics.CodeAnalysis;
using AppEngine.Enums;

namespace AppEngine.Extensions;

public static class GuidExtensions
{
    extension(Guid input)
    {
        public bool IsEmpty() => input == Guid.Empty;

        public bool IsNotEmpty() => !input.IsEmpty();

        public bool HasValue() => !input.IsEmpty();

        public Guid GetValueOrCreateNew() => input.IsEmpty() ? Guid.NewGuid() : input;

        public Guid GetValueOrDefault(Guid defaultValue) => input.IsEmpty() ? defaultValue : input;

        public Guid GetValueOrCreateNew(GuidVersion guidVersion)
            => input.IsEmpty() ? guidVersion switch
            {
                GuidVersion.Version7 => Guid.CreateVersion7(),
                _ => Guid.NewGuid()
            } : input;
    }

    extension([NotNullWhen(false)] Guid? input)
    {
        public bool IsEmpty() => input.GetValueOrDefault() == Guid.Empty;
    }

    extension([NotNullWhen(true)] Guid? input)
    {
        public bool IsNotEmpty() => !input.IsEmpty();

        public bool HasValue() => !input.IsEmpty();
    }

    extension(Guid? input)
    {
        public Guid GetValueOrCreateNew() => input.IsEmpty() ? Guid.NewGuid() : input!.Value;

        public Guid GetValueOrCreateNew(GuidVersion guidVersion) => input.IsEmpty() ? guidVersion switch
        {
            GuidVersion.Version7 => Guid.CreateVersion7(),
            _ => Guid.NewGuid()
        } : input!.Value;
    }
}