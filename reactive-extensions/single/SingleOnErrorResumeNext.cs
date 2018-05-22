using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using static akarnokd.reactive_extensions.ValidationHelper;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Switches to a fallback single source if
    /// the upstream fails.
    /// </summary>
    /// <typeparam name="T">The success value type.</typeparam>
    /// <remarks>Since 0.0.11</remarks>
    internal sealed class SingleOnErrorResumeNext<T> : ISingleSource<T>
    {
        readonly ISingleSource<T> source;

        readonly ISingleSource<T> fallback;

        public SingleOnErrorResumeNext(ISingleSource<T> source, ISingleSource<T> fallback)
        {
            this.source = source;
            this.fallback = fallback;
        }

        public void Subscribe(ISingleObserver<T> observer)
        {
            source.Subscribe(new OnErrorResumeNextObserver(observer, fallback));
        }

        sealed class OnErrorResumeNextObserver : ISingleObserver<T>, IDisposable
        {
            readonly ISingleObserver<T> downstream;

            IDisposable upstream;

            ISingleSource<T> fallback;

            IDisposable fallbackObserver;

            public OnErrorResumeNextObserver(ISingleObserver<T> downstream, ISingleSource<T> fallback)
            {
                this.downstream = downstream;
                this.fallback = fallback;
            }

            public void Dispose()
            {
                upstream.Dispose();
                DisposableHelper.Dispose(ref fallbackObserver);
            }

            public void OnError(Exception error)
            {
                var inner = new SingleInnerObserver<T>(downstream);
                if (Interlocked.CompareExchange(ref fallbackObserver, inner, null) == null)
                {
                    var fb = fallback;
                    fallback = null;

                    fb.Subscribe(inner);
                }
            }

            public void OnSubscribe(IDisposable d)
            {
                upstream = d;
                downstream.OnSubscribe(this);
            }

            public void OnSuccess(T item)
            {
                downstream.OnSuccess(item);
            }
        }
    }

    /// <summary>
    /// Switches to a fallback single source if
    /// the upstream fails.
    /// </summary>
    /// <typeparam name="T">The success value type.</typeparam>
    /// <remarks>Since 0.0.11</remarks>
    internal sealed class SingleOnErrorResumeNextSelector<T> : ISingleSource<T>
    {
        readonly ISingleSource<T> source;

        readonly Func<Exception, ISingleSource<T>> handler;

        public SingleOnErrorResumeNextSelector(ISingleSource<T> source, Func<Exception, ISingleSource<T>> handler)
        {
            this.source = source;
            this.handler = handler;
        }

        public void Subscribe(ISingleObserver<T> observer)
        {
            source.Subscribe(new OnErrorResumeNextObserver(observer, handler));
        }

        sealed class OnErrorResumeNextObserver : ISingleObserver<T>, IDisposable
        {
            readonly ISingleObserver<T> downstream;

            readonly Func<Exception, ISingleSource<T>> handler;

            IDisposable upstream;

            IDisposable fallbackObserver;

            public OnErrorResumeNextObserver(ISingleObserver<T> downstream, Func<Exception, ISingleSource<T>> handler)
            {
                this.downstream = downstream;
                this.handler = handler;
            }

            public void Dispose()
            {
                upstream.Dispose();
                DisposableHelper.Dispose(ref fallbackObserver);
            }

            public void OnError(Exception error)
            {
                var fb = default(ISingleSource<T>);

                try
                {
                    fb = RequireNonNullRef(handler(error), "The handler returned a null ISingleSource");
                }
                catch (Exception ex)
                {
                    downstream.OnError(new AggregateException(error, ex));
                    return;
                }

                var inner = new SingleInnerObserver<T>(downstream);
                if (Interlocked.CompareExchange(ref fallbackObserver, inner, null) == null)
                {
                    fb.Subscribe(inner);
                }
            }

            public void OnSubscribe(IDisposable d)
            {
                upstream = d;
                downstream.OnSubscribe(this);
            }

            public void OnSuccess(T item)
            {
                downstream.OnSuccess(item);
            }
        }
    }
}
