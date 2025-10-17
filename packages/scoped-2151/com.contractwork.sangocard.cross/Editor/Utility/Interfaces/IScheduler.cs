using System;

namespace SangoCard.Cross.Editor;

#if !HAS_NUGET_SYSTEM_REACTIVE
public interface IScheduler
{
    DateTimeOffset Now { get; }

    IDisposable Schedule<TState>(
        TState state,
        Func<IScheduler,TState, IDisposable> action);

    IDisposable Schedule<TState>(
        TState state,
        TimeSpan dueTime,
        Func<IScheduler, TState, IDisposable> action);

    IDisposable Schedule<TState>(
        TState state,
        DateTimeOffset dueTime,
        Func<IScheduler, TState, IDisposable> action);
}
#endif
