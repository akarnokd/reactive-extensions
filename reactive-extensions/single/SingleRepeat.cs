﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Subscribe to a single source repeatedly if it
    /// succeeds or completes normally and
    /// emit its success items.
    /// </summary>
    /// <typeparam name="T">The success value type</typeparam>
    internal sealed class SingleRepeat<T> : IObservable<T>
    {
        readonly ISingleSource<T> source;

        readonly long times;

        public SingleRepeat(ISingleSource<T> source, long times)
        {
            this.source = source;
            this.times = times;
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            var parent = new RepeatObserver(observer, source, times);
            parent.Next();
            return parent;
        }

        sealed class RepeatObserver : SingleRepeatObserver<T>
        {
            long times;

            internal RepeatObserver(IObserver<T> downstream, ISingleSource<T> source, long times) : base(downstream, source)
            {
                this.times = times;
            }

            public override void OnSuccess(T item)
            {
                downstream.OnNext(item);

                var t = times;
                if (t == long.MaxValue)
                {
                    Next();
                }
                else
                if (t != 0L)
                {
                    times = t - 1;
                    Next();
                }
                else
                {
                    downstream.OnCompleted();
                }
            }
        }
    }

    internal abstract class SingleRepeatObserver<T> : ISingleObserver<T>, IDisposable
    {
        protected readonly IObserver<T> downstream;

        protected readonly ISingleSource<T> source;

        protected IDisposable upstream;

        int wip;

        protected SingleRepeatObserver(IObserver<T> downstream, ISingleSource<T> source)
        {
            this.downstream = downstream;
            this.source = source;
        }

        public void Dispose()
        {
            DisposableHelper.Dispose(ref upstream);
        }

        public abstract void OnSuccess(T item);

        public virtual void OnError(Exception error)
        {
            DisposableHelper.WeakDispose(ref upstream);
            downstream.OnError(error);
        }

        public void OnSubscribe(IDisposable d)
        {
            DisposableHelper.Replace(ref upstream, d);
        }


        internal void Next()
        {
            if (Interlocked.Increment(ref wip) == 1)
            {
                for (; ; )
                {
                    if (!DisposableHelper.IsDisposed(ref upstream))
                    {
                        source.Subscribe(this);
                    }

                    if (Interlocked.Decrement(ref wip) == 0)
                    {
                        break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Subscribe to a single source repeatedly if it
    /// succeeds or completes normally and
    /// the given handler returns true.
    /// </summary>
    /// <typeparam name="T">The success value type.</typeparam>
    /// <remarks>Since 0.0.12</remarks>
    internal sealed class SingleRepeatPredicate<T> : IObservable<T>
    {
        readonly ISingleSource<T> source;

        readonly Func<long, bool> handler;

        public SingleRepeatPredicate(ISingleSource<T> source, Func<long, bool> handler)
        {
            this.source = source;
            this.handler = handler;
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            var parent = new RepeatObserver(observer, source, handler);
            parent.Next();
            return parent;
        }

        sealed class RepeatObserver : SingleRepeatObserver<T>
        {
            readonly Func<long, bool> handler;

            int times;

            internal RepeatObserver(IObserver<T> downstream, ISingleSource<T> source, Func<long, bool> handler) : base(downstream, source)
            {
                this.handler = handler;
            }

            public override void OnSuccess(T item)
            {
                downstream.OnNext(item);

                var b = false;
                try
                {
                    b = handler(++times);
                }
                catch (Exception ex)
                {
                    downstream.OnError(ex);
                    return;
                }

                if (b)
                {
                    Next();
                }
                else
                {
                    downstream.OnCompleted();
                }
            }
        }
    }
}
