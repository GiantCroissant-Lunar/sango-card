using System;
#if HAS_NUGET_SYSTEM_REACTIVE
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
#endif
using UnityEditor;

namespace SangoCard.Cross.Editor;

using IReactiveScheduler =
#if HAS_NUGET_SYSTEM_REACTIVE
    System.Reactive.Concurrency.IScheduler;
#else
    IScheduler;
#endif

public class UnityEditorMainThreadScheduler : IReactiveScheduler
{
    public DateTimeOffset Now => DateTimeOffset.Now;

    public IDisposable Schedule<TState>(
        TState state,
        Func<IReactiveScheduler, TState, IDisposable> action)
    {
        EditorApplication.delayCall += () => action(this, state);
#if HAS_NUGET_SYSTEM_REACTIVE
        return Disposable.Empty;
#else
        return NoopDisposable.Instance;
#endif
    }

    public IDisposable Schedule<TState>(
        TState state,
        DateTimeOffset dueTime,
        Func<IReactiveScheduler, TState, IDisposable> action)
    {
        // Unity Editor doesn't support delayed scheduling natively, so we just schedule on next update
        return Schedule(state, action);
    }

    public IDisposable Schedule<TState>(
        TState state,
        TimeSpan dueTime,
        Func<IReactiveScheduler, TState, IDisposable> action)
    {
        // Unity Editor doesn't support delayed scheduling natively, so we just schedule on next update
        return Schedule(state, action);
    }
}
