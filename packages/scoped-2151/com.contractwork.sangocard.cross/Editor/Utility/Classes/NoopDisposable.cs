using System;

namespace SangoCard.Cross.Editor;

#if !HAS_NUGET_SYSTEM_REACTIVE
public sealed class NoopDisposable : IDisposable
{
    public static readonly NoopDisposable Instance = new();

    private NoopDisposable()
    {
    }

    public void Dispose()
    {
    }
}
#endif
