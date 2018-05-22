using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Subscribe to a single source repeatedly (or up to a maximum
    /// number of times) if it keeps failing.
    /// </summary>
    /// <typeparam name="T">The success value type</typeparam>
    internal sealed class SingleRetry<T> : ISingleSource<T>
    {
        readonly ISingleSource<T> source;

        readonly long times;

        public SingleRetry(ISingleSource<T> source, long times)
        {
            this.source = source;
            this.times = times;
        }

        public void Subscribe(ISingleObserver<T> observer)
        {
            var parent = new RetryObserver(observer, source, times);
            observer.OnSubscribe(parent);
            parent.Next();
        }

        sealed class RetryObserver : SingleRetryObserver<T>
        {
            long times;

            internal RetryObserver(ISingleObserver<T> downstream, ISingleSource<T> source, long times) : base(downstream, source)
            {
                this.times = times;
            }

            public override void OnError(Exception ex)
            {
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
                    downstream.OnError(ex);
                }
            }
        }
    }

    internal abstract class SingleRetryObserver<T> : ISingleObserver<T>, IDisposable
    {
        protected readonly ISingleObserver<T> downstream;

        protected readonly ISingleSource<T> source;

        protected IDisposable upstream;

        int wip;

        protected SingleRetryObserver(ISingleObserver<T> downstream, ISingleSource<T> source)
        {
            this.downstream = downstream;
            this.source = source;
        }

        public void Dispose()
        {
            DisposableHelper.Dispose(ref upstream);
        }

        public void OnSuccess(T item)
        {
            DisposableHelper.WeakDispose(ref upstream);
            downstream.OnSuccess(item);
        }

        public abstract void OnError(Exception error);

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
    /// Subscribe to a single source repeatedly if it keeps failing
    /// and the handler returns true upon a failure.
    /// </summary>
    /// <typeparam name="T">The success value type.</typeparam>
    /// <remarks>Since 0.0.12</remarks>
    internal sealed class SingleRetryPredicate<T> : ISingleSource<T>
    {
        readonly ISingleSource<T> source;

        readonly Func<Exception, long, bool> handler;

        public SingleRetryPredicate(ISingleSource<T> source, Func<Exception, long, bool> handler)
        {
            this.source = source;
            this.handler = handler;
        }

        public void Subscribe(ISingleObserver<T> observer)
        {
            var parent = new RetryObserver(observer, source, handler);
            observer.OnSubscribe(parent);
            parent.Next();
        }

        sealed class RetryObserver : SingleRetryObserver<T>
        {
            readonly Func<Exception, long, bool> handler;

            int times;

            internal RetryObserver(ISingleObserver<T> downstream, ISingleSource<T> source, Func<Exception, long, bool> handler) : base(downstream, source)
            {
                this.handler = handler;
            }

            public override void OnError(Exception error)
            {
                var b = false;
                try
                {
                    b = handler(error, ++times);
                }
                catch (Exception ex)
                {
                    downstream.OnError(new AggregateException(error, ex));
                    return;
                }

                if (b)
                {
                    Next();
                }
                else
                {
                    downstream.OnError(error);
                }
            }
        }
    }
}
