using System;
using System.Reactive.Concurrency;
using System.Threading;

namespace akarnokd.reactive_extensions_test
{
    /// <summary>
    /// Test helper scheduler that executes actions on the current thread
    /// sleeping if necessary.
    /// </summary>
    internal sealed class ImmediateScheduler : IScheduler
    {
        public DateTimeOffset Now => DateTimeOffset.Now;

        internal static readonly IScheduler INSTANCE = new ImmediateScheduler();

        public IDisposable Schedule<TState>(TState state, Func<IScheduler, TState, IDisposable> action)
        {
            return action(this, state);
        }

        public IDisposable Schedule<TState>(TState state, TimeSpan dueTime, Func<IScheduler, TState, IDisposable> action)
        {
            Thread.Sleep(dueTime);
            return action(this, state);
        }

        public IDisposable Schedule<TState>(TState state, DateTimeOffset dueTime, Func<IScheduler, TState, IDisposable> action)
        {
            var diff = dueTime - Now;
            Thread.Sleep(diff);
            return action(this, state);
        }
    }
}
