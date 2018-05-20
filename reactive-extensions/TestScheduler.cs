using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{

    /// <summary>
    /// A scheduler implementation that uses virtual time and moves forward
    /// only when <see cref="AdvanceTimeBy(TimeSpan)"/> is invoked.
    /// </summary>
    /// <remarks>
    /// Since 0.0.6<br/>
    /// Note that the time resolution of this scheduler is milliseconds.
    /// </remarks>
    public sealed class TestScheduler : IScheduler
    {
        readonly SortedList<TestSchedulerTaskKey, Action> queue;

        long currentTimeMillis;

        long index;

        /// <summary>
        /// Constructs an empty TestScheduler with the optional initial virtual time.
        /// </summary>
        /// <param name="initialTimeMillis">The initial virtual time in milliseconds.</param>
        public TestScheduler(long initialTimeMillis = 0L)
        {
            this.queue = new SortedList<TestSchedulerTaskKey, Action>(TestScehdulerTaskKeyComparer.INSTANCE);
            Volatile.Write(ref currentTimeMillis, initialTimeMillis);
        }

        /// <summary>
        /// Returns the current virtual time.
        /// </summary>
        public DateTimeOffset Now => DateTimeOffset.FromUnixTimeMilliseconds(Volatile.Read(ref currentTimeMillis));

        /// <summary>
        /// Schedules the action to be executed immediately.
        /// </summary>
        /// <typeparam name="TState">The type state information to provide to the action.</typeparam>
        /// <param name="state">The state information provided to the action when it is executed.</param>
        /// <param name="action">The action to invoke with the current scheduler and the state.</param>
        /// <returns>The disposable that allows canceling this particular scheduled action.</returns>
        public IDisposable Schedule<TState>(TState state, Func<IScheduler, TState, IDisposable> action)
        {
            ValidationHelper.RequireNonNull(action, nameof(action));
            return ScheduleActual(state, Now, action);
        }

        /// <summary>
        /// Schedules the action to be executed after the specified
        /// time passed.
        /// </summary>
        /// <typeparam name="TState">The type state information to provide to the action.</typeparam>
        /// <param name="state">The state information provided to the action when it is executed.</param>
        /// <param name="dueTime">The relative time delay when the action should be invoked.</param>
        /// <param name="action">The action to invoke with the current scheduler and the state.</param>
        /// <returns>The disposable that allows canceling this particular scheduled action.</returns>
        public IDisposable Schedule<TState>(TState state, TimeSpan dueTime, Func<IScheduler, TState, IDisposable> action)
        {
            ValidationHelper.RequireNonNull(action, nameof(action));
            var now = Now;
            if (dueTime < TimeSpan.Zero)
            {
                return ScheduleActual(state, now, action);
            }
            return ScheduleActual(state, now + dueTime, action);
        }

        /// <summary>
        /// Schedules the action to be executed at the specified due date.
        /// </summary>
        /// <typeparam name="TState">The type state information to provide to the action.</typeparam>
        /// <param name="state">The state information provided to the action when it is executed.</param>
        /// <param name="dueTime">The absolute time delay when the action should be invoked.</param>
        /// <param name="action">The action to invoke with the current scheduler and the state.</param>
        /// <returns>The disposable that allows canceling this particular scheduled action.</returns>
        public IDisposable Schedule<TState>(TState state, DateTimeOffset dueTime, Func<IScheduler, TState, IDisposable> action)
        {
            var now = Now;
            if (dueTime < now)
            {
                return ScheduleActual(state, now, action);
            }
            return ScheduleActual(state, dueTime, action);
        }

        IDisposable ScheduleActual<TState>(TState state, DateTimeOffset dueTime, Func<IScheduler, TState, IDisposable> action)
        {
            var key = new TestSchedulerTaskKey(Interlocked.Increment(ref index), dueTime);
            Add(key, () => {
                key.SetNext(action(this, state));
            });
            return key;
        }

        /// <summary>
        /// Moves the virtual time forward by the specified relative time,
        /// executing all queued actions during this timespan.
        /// </summary>
        /// <param name="timespan">The timespan to advance the virtual time forward</param>
        public void AdvanceTimeBy(TimeSpan timespan)
        {
            AdvanceTimeBy((long)timespan.TotalMilliseconds);
        }

        /// <summary>
        /// Moves the virtual time forward by the specified relative time,
        /// executing all queued actions during this time window.
        /// </summary>
        /// <param name="timeInMillis">The time window in milliseconds to advance the virtual time forward</param>
        public void AdvanceTimeBy(long timeInMillis)
        {
            long now = Volatile.Read(ref currentTimeMillis);
            long end = now + timeInMillis;
            for (; ; )
            {
                if (TryPeek(out var key, out var task))
                {
                    if (key.IsDisposed())
                    {
                        Remove(key);
                        continue;
                    }
                    var due = key.due;
                    if (due <= end)
                    {
                        Volatile.Write(ref currentTimeMillis, due);
                        Remove(key);

                        task();
                        continue;
                    }
                }
                Volatile.Write(ref currentTimeMillis, end);
                break;
            }
        }

        /// <summary>
        /// Advances the virtual time until no more tasks are queued up.
        /// </summary>
        public void RunAll()
        {
            for (; ; )
            {
                if (TryPeek(out var key, out var task))
                {
                    if (key.IsDisposed())
                    {
                        Remove(key);
                    }
                    var due = key.due;
                    Volatile.Write(ref currentTimeMillis, due);
                    Remove(key);

                    task();
                }
                else
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Returns true if more tasks are queued up.
        /// </summary>
        /// <returns>True if more tasks are queued up.</returns>
        public bool HasTasks()
        {
            lock (this)
            {
                return queue.Count != 0;
            }
        }

        void Add(TestSchedulerTaskKey key, Action task)
        {
            lock (this)
            {
                queue.Add(key, task);
            }
        }

        bool TryPeek(out TestSchedulerTaskKey key, out Action task)
        {
            lock (this)
            {
                if (queue.Count == 0)
                {
                    key = default(TestSchedulerTaskKey);
                    task = default(Action);
                    return false;
                }
                key = queue.Keys[0];
                task = queue.Values[0];
                return true;
            }
        }

        void Remove(TestSchedulerTaskKey key)
        {
            lock (this)
            {
                queue.Remove(key);
            }
        }

        internal sealed class TestSchedulerTaskKey : IDisposable
        {
            internal readonly long index;
            internal readonly long due;

            IDisposable next;

            public TestSchedulerTaskKey(long index, DateTimeOffset due)
            {
                this.index = index;
                this.due = due.ToUnixTimeMilliseconds();
            }

            public void Dispose()
            {
                DisposableHelper.Dispose(ref next);
            }

            internal bool IsDisposed()
            {
                return DisposableHelper.IsDisposed(ref next);
            }

            internal void SetNext(IDisposable next)
            {
                if (next != DisposableHelper.EMPTY)
                {
                    DisposableHelper.Replace(ref this.next, next);
                }
            }
        }

        internal sealed class TestScehdulerTaskKeyComparer : IComparer<TestSchedulerTaskKey>
        {
            internal static readonly TestScehdulerTaskKeyComparer INSTANCE =
                new TestScehdulerTaskKeyComparer();

            public int Compare(TestSchedulerTaskKey x, TestSchedulerTaskKey y)
            {
                var c = x.due.CompareTo(y.due);
                if (c == 0)
                {
                    c = x.index.CompareTo(y.index);
                }
                return c;
            }
        }
    }
}
